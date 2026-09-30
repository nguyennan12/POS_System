using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Invoices.Commands.GenerateInvoice;

// Internal use case: the caller owns the transaction and saves the pending invoice.
public sealed record GenerateInvoiceCommand(Guid OrderId) : ICommand;
