using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Valour.MPS.Config;
using Valour.MPS.Proxy;

namespace Valour.MPS.Database
{
    public class MediaDB : DbContext
    {
        public static string ConnectionString = $"server={VMPS_Config.Current.Database_Address};" +
                                                $"database={VMPS_Config.Current.Database_Name};" +
                                                $"uid={VMPS_Config.Current.Database_User};" +
                                                $"pwd={VMPS_Config.Current.Database_Password};" +
                                                $"port=3306;" +
                                                $"SslMode=Required;";

        /// <summary>
        /// This is only here to fulfill the need of the constructor.
        /// It does literally nothing at all.
        /// </summary>
        public static DbContextOptions DBOptions;

        public DbSet<ProxyItem> ProxyItems { get; set; }

        public MediaDB(DbContextOptions options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql(ConnectionString, ServerVersion.Parse("8.0.20-mysql"), options => options.EnableRetryOnFailure());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasCharSet(CharSet.Utf8Mb4);
        }
    }
}
