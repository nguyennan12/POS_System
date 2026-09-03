using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Domain.Chatbot;
using POS.Domain.Chatbot.Enums;

namespace POS.Infrastructure.Persistence.Configurations;

public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(c => c.SessionId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.StartedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(c => c.SessionId).IsUnique();
        builder.HasOne(c => c.Store).WithMany().HasForeignKey(c => c.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.ConfigureUuidPrimaryKey();
        builder.Property(c => c.Sender).HasConversion<string>().IsRequired().HasMaxLength(10);
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(c => new { c.ConversationId, c.CreatedAt });
        builder.HasOne(c => c.Conversation).WithMany().HasForeignKey(c => c.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasTableCheckConstraint("ck_chat_messages_sender", "sender IN ('Customer','Bot')");
    }
}
