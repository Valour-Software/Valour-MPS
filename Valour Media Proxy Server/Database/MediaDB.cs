using Microsoft.EntityFrameworkCore;
using Valour.MPS.Config;
using Valour.MPS.Media;
using Valour.MPS.Proxy;

namespace Valour.MPS.Database
{
    public class MediaDb : DbContext
    {
        public static string ConnectionString = $"Host={VmpsConfig.Current.DbAddr};" +
                                                $"Database={VmpsConfig.Current.DbName};" +
                                                $"Username={VmpsConfig.Current.DbUser};" +
                                                $"Password={VmpsConfig.Current.DbPass};" +
                                                $"SslMode=Prefer;";

        /// <summary>
        /// This is only here to fulfill the need of the constructor.
        /// It does literally nothing at all.
        /// </summary>
        public static DbContextOptions DBOptions;

        public DbSet<ProxyItem> ProxyItems { get; set; }
        public DbSet<BucketItem> BucketItems { get; set; }

        public MediaDb(DbContextOptions options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
