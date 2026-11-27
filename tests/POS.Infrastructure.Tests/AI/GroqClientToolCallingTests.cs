using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using POS.Application.Abstractions.AI;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Infrastructure.AI.Groq;

namespace POS.Infrastructure.Tests.AI;

public sealed class GroqClientToolCallingTests
{
    [Fact]
    public async Task GenerateAsync_TextOnlyCompleted_OmitsToolsAndReturnsCompletedText()
    {
        string? capturedRequestJson = null;
        var client = CreateClient((req, _) =>
        {
            capturedRequestJson = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(HttpStatusCode.OK, """
            {
              "id": "chatcmpl-text-1",
              "choices": [
                {
                  "finish_reason": "stop",
                  "message": {
                    "role": "assistant",
                    "content": "Xin chào! Tôi có thể hỗ trợ gì cho bạn?"
                  }
                }
              ],
              "usage": { "prompt_tokens": 25, "completion_tokens": 12 }
            }
            """);
        });

        var response = await client.GenerateAsync(
            new AiGenerationRequest("System prompt", [AiConversationTurn.User("Xin chào")]),
            CancellationToken.None);

        response.Status.Should().Be(AiModelStatus.Completed);
        response.Text.Should().Be("Xin chào! Tôi có thể hỗ trợ gì cho bạn?");
        response.ToolCalls.Should().BeEmpty();
        response.ProviderRequestId.Should().Be("chatcmpl-text-1");

        using var reqDoc = JsonDocument.Parse(capturedRequestJson!);
        reqDoc.RootElement.TryGetProperty("tools", out _).Should().BeFalse();
        reqDoc.RootElement.TryGetProperty("tool_choice", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_WithTools_ParsesMultipleToolCalls_AndSerializesContinuationMessages()
    {
        var requests = new List<string>();
        var callIndex = 0;

        var client = CreateClient((req, _) =>
        {
            requests.Add(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            callIndex++;

            if (callIndex == 1)
            {
                return JsonResponse(HttpStatusCode.OK, """
                {
                  "id": "chatcmpl-tool-1",
                  "choices": [
                    {
                      "finish_reason": "tool_calls",
                      "message": {
                        "role": "assistant",
                        "content": null,
                        "tool_calls": [
                          {
                            "id": "call_p1",
                            "type": "function",
                            "function": {
                              "name": "search_products",
                              "arguments": "{\"keyword\":\"Coca 330ml\",\"limit\":5}"
                            }
                          },
                          {
                            "id": "call_s1",
                            "type": "function",
                            "function": {
                              "name": "get_stock_alerts",
                              "arguments": "{\"nearExpiryDays\":15}"
                            }
                          }
                        ]
                      }
                    }
                  ],
                  "usage": { "prompt_tokens": 150, "completion_tokens": 40 }
                }
                """);
            }

            return JsonResponse(HttpStatusCode.OK, """
            {
              "id": "chatcmpl-tool-2",
              "choices": [
                {
                  "finish_reason": "stop",
                  "message": {
                    "role": "assistant",
                    "content": "Coca 330ml hiện còn 48 lon trong kho."
                  }
                }
              ],
              "usage": { "prompt_tokens": 220, "completion_tokens": 18 }
            }
            """);
        });

        var tools = new AiToolRegistry().GetAvailableTools();

        // Lượt 1: gửi câu hỏi + 22 tools
        var firstResponse = await client.GenerateAsync(
            new AiGenerationRequest("System", [AiConversationTurn.User("Coca 330ml còn bao nhiêu?")], tools),
            CancellationToken.None);

        firstResponse.Status.Should().Be(AiModelStatus.RequiresAction);
        firstResponse.Text.Should().BeNull();
        firstResponse.ToolCalls.Should().HaveCount(2);
        firstResponse.ToolCalls[0].Id.Should().Be("call_p1");
        firstResponse.ToolCalls[0].Name.Should().Be("search_products");
        firstResponse.ToolCalls[0].Arguments.GetProperty("keyword").GetString().Should().Be("Coca 330ml");
        firstResponse.ToolCalls[1].Id.Should().Be("call_s1");
        firstResponse.ToolCalls[1].Arguments.GetProperty("nearExpiryDays").GetInt32().Should().Be(15);

        using (var firstReqDoc = JsonDocument.Parse(requests[0]))
        {
            firstReqDoc.RootElement.GetProperty("tool_choice").GetString().Should().Be("auto");
            firstReqDoc.RootElement.GetProperty("tools").GetArrayLength().Should().Be(22);
        }

        // Lượt 2: gửi kèm assistant(tool_calls) + 2 message role="tool"
        var continuationMessages = new[]
        {
            AiConversationTurn.User("Coca 330ml còn bao nhiêu?"),
            AiConversationTurn.AssistantToolCalls(firstResponse.Text, firstResponse.ToolCalls),
            AiConversationTurn.Tool("call_p1", "search_products", """{"success":true,"data":[{"qtyOnHand":48}],"resultCount":1}"""),
            AiConversationTurn.Tool("call_s1", "get_stock_alerts", """{"success":true,"data":[],"resultCount":0}""")
        };

        var secondResponse = await client.GenerateAsync(
            new AiGenerationRequest("System", continuationMessages, tools),
            CancellationToken.None);

        secondResponse.Status.Should().Be(AiModelStatus.Completed);
        secondResponse.Text.Should().Contain("48 lon");

        using (var secondReqDoc = JsonDocument.Parse(requests[1]))
        {
            var msgs = secondReqDoc.RootElement.GetProperty("messages");
            msgs.GetArrayLength().Should().Be(5); // system + user + assistant(tool_calls) + 2 tool results
            msgs[2].GetProperty("role").GetString().Should().Be("assistant");
            msgs[2].GetProperty("tool_calls").GetArrayLength().Should().Be(2);
            msgs[3].GetProperty("role").GetString().Should().Be("tool");
            msgs[3].GetProperty("tool_call_id").GetString().Should().Be("call_p1");
            msgs[4].GetProperty("tool_call_id").GetString().Should().Be("call_s1");
        }
    }

    [Fact]
    public async Task GenerateAsync_GroqToolUseFailed400_MapsToSafeAiToolUseFailed_WithoutLeakingFailedGeneration()
    {
        var client = CreateClient((_, _) => JsonResponse(HttpStatusCode.BadRequest, """
        {
          "error": {
            "message": "Failed to call a function.",
            "type": "invalid_request_error",
            "code": "tool_use_failed",
            "failed_generation": "<function=search_products>{\"secret\":\"leak\"}</function>"
          }
        }
        """));

        var act = () => client.GenerateAsync(
            new AiGenerationRequest("System", [AiConversationTurn.User("Test")]),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AiModelException>();
        ex.Which.Code.Should().Be("AI_TOOL_USE_FAILED");
        ex.Which.StatusCode.Should().Be(502);
        ex.Which.Message.Should().NotContain("secret");
        ex.Which.Message.Should().NotContain("failed_generation");
    }

    [Fact]
    public async Task GenerateAsync_InvalidToolArgumentsJson_ThrowsAiInvalidResponse()
    {
        var client = CreateClient((_, _) => JsonResponse(HttpStatusCode.OK, """
        {
          "id": "chatcmpl-bad",
          "choices": [
            {
              "finish_reason": "tool_calls",
              "message": {
                "role": "assistant",
                "tool_calls": [
                  {
                    "id": "call_bad",
                    "type": "function",
                    "function": {
                      "name": "search_products",
                      "arguments": "{not-valid-json"
                    }
                  }
                ]
              }
            }
          ]
        }
        """));

        var act = () => client.GenerateAsync(
            new AiGenerationRequest("System", [AiConversationTurn.User("Test")]),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AiModelException>();
        ex.Which.Code.Should().Be("AI_INVALID_RESPONSE");
    }

    private static GroqClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/")
        };

        var options = Options.Create(new GroqOptions
        {
            Enabled = true,
            ApiKey = "gsk_test_dummy_key",
            BaseUrl = "https://api.groq.com/openai/v1/",
            Model = "llama-3.3-70b-versatile",
            Temperature = 0.1,
            MaxCompletionTokens = 1200
        });

        return new GroqClient(httpClient, options, NullLogger<GroqClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request, cancellationToken));
    }
}
