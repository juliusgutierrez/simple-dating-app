using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Interfaces;
using API.Services;
using DatingApp.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, 
            IConfiguration config, string myAllowSpecificOrigins) 
        {
            services.AddDbContext<DataContext>(opt => 
            {
                opt.UseSqlite(config.GetConnectionString("DefaultConnection"));
            });
            services.AddScoped<ITokenService, TokenService>();

            services.AddCors(options =>
            {
                options.AddPolicy(name: myAllowSpecificOrigins, 
                                builder =>
                                {
                                    builder.WithOrigins("http://localhost:4200",
                                                        "https://localhost:4200");
                                });
            });

            return services;
        }
    }
}