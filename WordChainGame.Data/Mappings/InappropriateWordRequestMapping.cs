

namespace WordChainGame.Data.Mappings
{
    using System.Data.Entity.ModelConfiguration;
    using WordChainGame.Data.Entities;
    public class InappropriateWordRequestMapping : EntityTypeConfiguration<InappropriateWordRequest>
    {
        public InappropriateWordRequestMapping()
        { 
            this.ToTable("InappropriateWordRequests");

            this.HasKey(p => p.Id);

            this.Property(p => p.DateCreated)
                .IsRequired();

            this.Property(p => p.IsInappropriate)
                .IsOptional();

            this.HasRequired(p => p.InappropriateWord)
                .WithMany(p => p.InappropriateWordRequests)
                .HasForeignKey(p => p.InappropriateWordId);

            this.HasRequired(p => p.Requester)
                .WithMany(p => p.InappropriateWordRequests)
                .HasForeignKey(p => p.RequesterId);
            
        }
    }
}
