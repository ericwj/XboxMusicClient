// Copyright (c) Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Xbox.Music.Platform.Client;
using Microsoft.Xbox.Music.Platform.Contract.AuthenticationDataModel;
using Microsoft.Xbox.Music.Platform.Contract.DataModel;
using Xunit;

namespace Tests
{
    public abstract class TestBase
    {
        /// <summary>
        /// A colon (':') separated JSON element path of a JSON object that contains
        /// the same properties as <see cref="XboxMusicClientOptions"/>.
        /// Those are read from the User Secrets path
        /// %APPDATA%\Roaming\Microsoft\UserSecrets\[UserSecretsId from project.json]\secrets.json
        /// </summary>
        public const string ConfigurationSectionPath = "XboxMusic";
        private static readonly Lazy<IConfigurationRoot> configuration = new Lazy<IConfigurationRoot>(
            () => {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddUserSecrets();
                return builder.Build();
            });
        private static readonly Lazy<XboxMusicClientOptions> options = new Lazy<XboxMusicClientOptions>(
            () => {
                var section = configuration.Value.GetSection(ConfigurationSectionPath);
                var ding = new ConfigureFromConfigurationOptions<XboxMusicClientOptions>(section);
                var result = new XboxMusicClientOptions();
                ding.Configure(result);
                return result;
            });

        protected static IXboxMusicClient Client { get; private set; }
        protected static IConfigurationRoot Configuration { get { return configuration.Value; } }
        public static XboxMusicClientOptions DefaultOptions { get { return options.Value; } }

        protected static IXboxMusicClient AuthenticatedClient
        {
            get
            {
                string xtoken = DefaultOptions.XToken;
                Assert.False(String.IsNullOrEmpty(xtoken) || xtoken.Length < 20,
                    "The XToken header value should be set in App.config for user authenticated tests");
                return Client.CreateUserAuthenticatedClient(new TestXToken {AuthorizationHeaderValue = xtoken});
            }
        }

        // When implementing real calls, use a unique stable client instance id per client id.
        // See XboxMusicClientRtExtensions for an example when using Windows Runtime.
        protected static string ClientInstanceId { get { return "XboxMusicClientTests12345678901234567890"; } }

        static TestBase()
        {
            string clientid = DefaultOptions.ClientId;
            string clientsecret = DefaultOptions.ClientSecret;
            var path = Microsoft.Extensions.Configuration.UserSecrets.PathHelper.GetSecretsPath(Directory.GetCurrentDirectory());
            Assert.False(string.IsNullOrEmpty(clientid), $"JSON element {ConfigurationSectionPath}:{nameof(XboxMusicClientOptions.ClientId)} must be set in user secrets.");
            Assert.False(string.IsNullOrEmpty(clientsecret), $"JSON element {ConfigurationSectionPath}:{nameof(XboxMusicClientOptions.ClientSecret)} must be set in user secrets.");
            Assert.NotNull(clientsecret); // "The client secret should be set in App.config"
            Assert.False(clientsecret.Contains("%"), "The client secret should not be URL encoded");
            Client = XboxMusicClientFactory.CreateXboxMusicClient(clientid, clientsecret);
        }

        protected void AssertPaginatedListIsValid<TContent>(PaginatedList<TContent> list, int minItems,
            int? minTotalItems = null)
        {
            Assert.NotNull(list); // "Results should contain " + typeof (TContent)
            Assert.NotNull(list.Items); // "Results should contain " + typeof (TContent) + " items"
            Assert.True(minItems <= list.Items.Count,
                "Results should contain more than " + minItems + " " + typeof (TContent) + " items");
            if (minTotalItems != null)
                Assert.True(minTotalItems <= list.TotalItemCount,
                    "The total number of  " + typeof (TContent) + " should be greater than " + minTotalItems);
        }

        private class TestXToken : IXToken
        {
            public string AuthorizationHeaderValue { get; set; }
            public DateTime NotAfter { get; set; }
            public DateTime IssueInstant { get; set; }

            public Task<bool> RefreshAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(false);
            }
        }
    }
}
