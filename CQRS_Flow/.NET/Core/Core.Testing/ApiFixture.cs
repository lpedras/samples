using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Testing
{
    public abstract class ApiFixture<TStartup>: ApiFixture where TStartup : class
    {
        public override TestContext CreateTestContext() =>
            new TestContext<TStartup>(GetConfiguration, SetupServices, SetupWebHostBuilder);
    }

    public abstract class ApiFixture: IAsyncLifetime
    {
        protected readonly TestContext Sut;

        private HttpClient Client => Sut.Client;

        protected abstract string ApiUrl { get; }

        protected virtual Dictionary<string, string> GetConfiguration(string fixtureName) => new();

        protected virtual Action<IServiceCollection>? SetupServices => null;

        protected virtual Func<IWebHostBuilder, IWebHostBuilder>? SetupWebHostBuilder => null;

        protected ApiFixture()
        {
            Sut = CreateTestContext();
        }

        public virtual TestContext CreateTestContext() => new(GetConfiguration, SetupServices, SetupWebHostBuilder);

        public virtual Task InitializeAsync() => Task.CompletedTask;

        public virtual Task DisposeAsync() => Task.CompletedTask;

        public async Task<HttpResponseMessage> Get(string path = "", int maxNumberOfRetries = 0, int retryIntervalInMs = 1000)
        {
            HttpResponseMessage queryResponse;
            var retryCount = maxNumberOfRetries;
            do
            {
                queryResponse = await Client.GetAsync(
                    $"{ApiUrl}/{path}"
                );

                if (queryResponse.StatusCode != HttpStatusCode.OK && retryCount > 0)
                    Thread.Sleep(retryIntervalInMs);
            } while (queryResponse.StatusCode != HttpStatusCode.OK && maxNumberOfRetries-- > 0);
            return queryResponse;
        }

        public Task<HttpResponseMessage> Post(string path, object request)
        {
            return Client.PostAsync(
                $"{ApiUrl}/{path}",
                request.ToJsonStringContent()
            );
        }

        public Task<HttpResponseMessage> Post(object request)
        {
            return Post(string.Empty, request);
        }
    }
}