﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Wyam.Common;
using Wyam.Common.Configuration;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules.Contents;
using Wyam.Core.Modules.Contents;
using Wyam.Testing;
using Wyam.Testing.Documents;
using Wyam.Testing.Execution;

namespace Wyam.Core.Tests.Modules.Contents
{
    [TestFixture]
    [NonParallelizable]
    public class SitemapFixture : BaseFixture
    {
        public class ExecuteTests : SitemapFixture
        {
            [TestCase("www.example.org", null, "http://www.example.org/sub/testfile")]
            [TestCase(null, "http://www.example.com", "http://www.example.com")]
            [TestCase("www.example.org", "http://www.example.com/{0}", "http://www.example.com/sub/testfile.html")]
            public async Task SitemapGeneratedWithSitemapItem(string hostname, string formatterString, string expected)
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Settings[Keys.LinkHideExtensions] = true;
                if (!string.IsNullOrWhiteSpace(hostname))
                {
                    context.Settings[Keys.Host] = hostname;
                }

                TestDocument doc = new TestDocument(new FilePath("sub/testfile.html"), "Test");
                IDocument[] inputs = { doc };

                Core.Modules.Metadata.Meta m = new Core.Modules.Metadata.Meta(
                    Keys.SitemapItem,
                    Config.FromDocument(d => new SitemapItem(d.Destination.FullPath)));

                Func<string, string> formatter = null;

                if (!string.IsNullOrWhiteSpace(formatterString))
                {
                    formatter = f => string.Format(formatterString, f);
                }

                Sitemap sitemap = new Sitemap(formatter);

                // When
                TestDocument result = await ExecuteAsync(doc, context, m, sitemap).SingleAsync();

                // Then
                result.Content.ShouldContain($"<loc>{expected}</loc>");
            }

            [TestCase("www.example.org", null, "http://www.example.org/sub/testfile")]
            [TestCase(null, "http://www.example.com", "http://www.example.com")]
            [TestCase("www.example.org", "http://www.example.com/{0}", "http://www.example.com/sub/testfile.html")]
            public async Task SitemapGeneratedWithSitemapItemAsString(string hostname, string formatterString, string expected)
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Settings[Keys.LinkHideExtensions] = true;
                if (!string.IsNullOrWhiteSpace(hostname))
                {
                    context.Settings[Keys.Host] = hostname;
                }

                TestDocument doc = new TestDocument(new FilePath("sub/testfile.html"), "Test");
                IDocument[] inputs = { doc };

                Core.Modules.Metadata.Meta m = new Core.Modules.Metadata.Meta(
                    Keys.SitemapItem,
                    Config.FromDocument(d => d.Destination.FullPath));

                Func<string, string> formatter = null;

                if (!string.IsNullOrWhiteSpace(formatterString))
                {
                    formatter = f => string.Format(formatterString, f);
                }

                Sitemap sitemap = new Sitemap(formatter);

                // When
                TestDocument result = await ExecuteAsync(doc, context, m, sitemap).SingleAsync();

                // Then
                result.Content.ShouldContain($"<loc>{expected}</loc>");
            }

            [TestCase("www.example.org", null, "http://www.example.org/sub/testfile")]
            [TestCase(null, "http://www.example.com", "http://www.example.com")]
            [TestCase("www.example.org", "http://www.example.com{0}", "http://www.example.com/sub/testfile")]
            public async Task SitemapGeneratedWhenNoSitemapItem(string hostname, string formatterString, string expected)
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                context.Settings[Keys.LinkHideExtensions] = true;
                if (!string.IsNullOrWhiteSpace(hostname))
                {
                    context.Settings[Keys.Host] = hostname;
                }

                TestDocument doc = new TestDocument(new FilePath("sub/testfile.html"), "Test");

                Func<string, string> formatter = null;

                if (!string.IsNullOrWhiteSpace(formatterString))
                {
                    formatter = f => string.Format(formatterString, f);
                }

                Sitemap sitemap = new Sitemap(formatter);

                // When
                TestDocument result = await ExecuteAsync(doc, context, sitemap).SingleAsync();

                // Then
                result.Content.ShouldContain($"<loc>{expected}</loc>");
            }
        }
    }
}
