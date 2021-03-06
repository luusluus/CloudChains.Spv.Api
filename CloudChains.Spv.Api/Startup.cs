using AutoMapper;
using Blocknet.Lib.CoinConfig;
using Blocknet.Lib.Services.Coins.Blocknet;
using CloudChains.Spv.Lib.Services.Coins.Alqocoin;
using CloudChains.Spv.Lib.Services.Coins.Base;
using CloudChains.Spv.Lib.Services.Coins.BitBay;
using CloudChains.Spv.Lib.Services.Coins.Bitcoin;
using CloudChains.Spv.Lib.Services.Coins.Blocknet;
using CloudChains.Spv.Lib.Services.Coins.Dash;
using CloudChains.Spv.Lib.Services.Coins.Digibyte;
using CloudChains.Spv.Lib.Services.Coins.Dogecoin;
using CloudChains.Spv.Lib.Services.Coins.Litecoin;
using CloudChains.Spv.Lib.Services.Coins.Pivx;
using CloudChains.Spv.Lib.Services.Coins.Polis;
using CloudChains.Spv.Lib.Services.Coins.Ravencoin;
using CloudChains.Spv.Lib.Services.Coins.Syscoin;
using CloudChains.Spv.Lib.Services.Coins.TrezarCoin;
using CloudChainsSpvWallet.Api.Core.Models;
using CloudChainsSpvWallet.Api.ExceptionHandling;
using CloudChainsSpvWallet.Api.Hubs;
using CloudChainsSpvWallet.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using XRouter.Api.Mapper;

namespace CloudChainsSpvWallet.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy("CorsPolicy", configurePolicy => configurePolicy
                    .WithOrigins("http://localhost:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddSignalR();

            var apiSettings = Configuration.GetSection("Api").Get<ApiSettings>();
            services.AddHttpClient("xcloud", c =>
            {
                c.BaseAddress = new Uri(apiSettings.XCloudBaseAddress + "/api/xrs/");
            });

            services.AddHttpClient("coininfo", c =>
            {
                c.BaseAddress = new Uri(apiSettings.CoinInfoBaseAddress + "/api/coininfo/");
            });

            services.AddControllers();

            var rpcSettings = Configuration.GetSection("XRouterConfig").Get<CoinRpcConfig>();

            services.AddTransient<IXRouterService>(service =>
                new XRouterService(
                    rpcSettings.Blocknet.DaemonUrl,
                    rpcSettings.Blocknet.RpcUserName,
                    rpcSettings.Blocknet.RpcPassword,
                    rpcSettings.Blocknet.WalletPassword,
                    rpcSettings.Blocknet.RpcRequestTimeoutInSeconds
                    )
            );
            services.AddTransient<ICoinService>(service =>
                new BitcoinService()
            );
            services.AddTransient<ICoinService>(service =>
                new LitecoinService()
            );

            services.AddTransient<ICoinService>(service =>
                new AlqocoinService()
            );

            services.AddTransient<ICoinService>(service =>
                new BitbayService()
            );

            services.AddTransient<ICoinService>(service =>
                new DashService()
            );

            services.AddTransient<ICoinService>(service =>
                new DigibyteService()
            );

            services.AddTransient<ICoinService>(service =>
                new DogecoinService()
            );

            services.AddTransient<ICoinService>(service =>
                new PivxService()
            );

            services.AddTransient<ICoinService>(service =>
                new PolisService()
            );

            services.AddTransient<ICoinService>(service =>
                new RavencoinService()
            );

            services.AddTransient<ICoinService>(service =>
                new SyscoinService()
            );

            services.AddTransient<ICoinService>(service =>
                new BlocknetzService()
            );

            services.AddTransient<ICoinService>(service =>
                new TrezarcoinService()
            );

            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new CloudChainsSpvWalletMappingProfile());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new JsonExceptionMiddleware().Invoke
            });

            app.UseMiddleware<CheckCoinMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ListUnspentHub>("/data");
            });
        }
    }
}
