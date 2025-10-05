using Fcg.Payment.Domain.Common.EventSourcing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Payment.Infrastructure.Configurations;

internal class EventModelConfigurations : IEntityTypeConfiguration<EventModel>
{
	public void Configure(EntityTypeBuilder<EventModel> builder)
	{
		builder.ToTable("EventStore");

		builder.HasKey(e => e.Id);

		builder.Property(e => e.EventType).HasConversion<int>().IsRequired();
		builder.Property(e => e.EventData).IsRequired();
		builder.Property(e => e.DateCreated).IsRequired();
		builder.Property(e => e.StreamId).IsRequired();
	}
}
