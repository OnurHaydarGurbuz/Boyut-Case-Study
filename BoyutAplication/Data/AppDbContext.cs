using BoyutAplication.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoyutAplication.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<InvoiceStatusLog> InvoiceStatusLogs => Set<InvoiceStatusLog>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InvoiceStatusLog>(entity =>
            {
                entity.ToTable("INVOICE_STATUS_LOG");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.InvoiceNumber)
                      .HasColumnName("INVOICE_NUMBER")
                      .IsRequired();

                entity.Property(x => x.TaxNumber)
                      .HasColumnName("TAX_NUMBER")
                      .IsRequired();

                entity.Property(x => x.ResponseCode)
                      .HasColumnName("RESPONSE_CODE")
                      .IsRequired();

                entity.Property(x => x.ResponseMessage)
                      .HasColumnName("RESPONSE_MESSAGE");

                entity.Property(x => x.RequestTime)
                      .HasColumnName("REQUEST_TIME")
                      .IsRequired();
            });
        }
    }
}
