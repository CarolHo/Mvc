// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class BasicViewsTest : IClassFixture<MvcTestFixture<BasicViews.Startup>>
    {
        public BasicViewsTest(MvcTestFixture<BasicViews.Startup> fixture)
        {
            // In Production (the MvcTestFixture default), site uses asp-fallback-* attributes. The generated HTML then
            // includes <script> elements containing non-HTML-encoded JavaScript e.g. a && b. XDocument doesn't know
            // <script> elements are implicitly CDATA content, leading to an XmlExcetion in AntiforgeryTestHelper.
            var innnerFixture = fixture.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

            Client = innnerFixture.CreateClient();
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("/")]
        [InlineData("/Home/HtmlHelpers")]
        public async Task Get_ReturnsOkAndAntiforgeryToken(string path)
        {
            // Arrange & Act
            var response = await Client.GetAsync(path);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var html = await response.Content.ReadAsStringAsync();
            Assert.NotNull(html);
            Assert.NotEmpty(html);

            // DTD declaration confuses XDocument.
            html = html.Replace("<!DOCTYPE html>", string.Empty);

            var token = AntiforgeryTestHelper.RetrieveAntiforgeryToken(html, "/");
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Home/HtmlHelpers")]
        public async Task Post_ReturnsOkAndNewPerson(string path)
        {
            // Arrange & Act 1
            var html = await Client.GetStringAsync(path);

            // Assert 1 (guard)
            Assert.NotEmpty(html);

            // Arrange 2
            // DTD declaration confuses XDocument.
            html = html.Replace("<!DOCTYPE html>", string.Empty);

            var token = AntiforgeryTestHelper.RetrieveAntiforgeryToken(html, "/");
            var name = Guid.NewGuid().ToString();
            name = name.Substring(startIndex: 0, length: name.LastIndexOf('-'));
            var form = new Dictionary<string, string>
            {
                { "__RequestVerificationToken", token },
                { "Age", "12" },
                { "BirthDate", "2006-03-01T09:51:43.041-07:00" },
                { "Name", name },
            };

            var content = new FormUrlEncodedContent(form);
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = content,
            };

            // Act 2
            var response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Contains($@"value=""{name}""", body);
        }
    }
}
