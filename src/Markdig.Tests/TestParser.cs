// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Extensions.JiraLinks;
using NUnit.Framework;

namespace Markdig.Tests
{
    public class TestParser
    {

//        [TestCase(@"> hello <a name=""n""
//> href=""javascript:alert('xss')"" > *you*</a>",
//"<blockquote>\n<p>hello &lt;a name=&quot;n&quot;\nhref=&quot;javascript:alert('xss')&quot; &gt; <em>you</em>&lt;/a&gt;</p>\n</blockquote>\n", true)]
        [TestCase(@"> hello <a name=""n""
> href=""javascript:alert('xss')"" > *you*</a>",
"<blockquote>\n<p>hello &lt;a name=&quot;n&quot;\nhref=&quot;javascript:alert('xss')&quot; &gt; <em>you</em>&lt;/a&gt;</p>\n</blockquote>\n", false)]
        public void ParseMarkdownWithDisabledHtml(string data, string expected, bool disableHtml)
        {
            var builder = new MarkdownPipelineBuilder();
            if (disableHtml)
            {
                builder.DisableHtml();
            }
            var parsedResults = Markdown.ToHtml(data, builder.Build());

            Assert.AreEqual(expected, parsedResults);
        }

        [Test]
        public void TestEmphasisAndHtmlEntity()
        {
            var markdownText = "*Unlimited-Fun&#174;*&#174;";
            TestSpec(markdownText, "<p><em>Unlimited-Fun®</em>®</p>");
        }

        [Test]
        public void TestThematicInsideCodeBlockInsideList()
        {
            var input = @"1. In the :

   ```
   Id                                   DisplayName         Description
   --                                   -----------         -----------
   62375ab9-6b52-47ed-826b-58e47e0e304b Group.Unified       ...
   ```";
            TestSpec(input, @"<ol>
<li><p>In the :</p>
<pre><code>Id                                   DisplayName         Description
--                                   -----------         -----------
62375ab9-6b52-47ed-826b-58e47e0e304b Group.Unified       ...
</code></pre></li>
</ol>");
        }

        public static void TestSpec(string inputText, string expectedOutputText, string extensions = null)
        {
            foreach (var pipeline in GetPipeline(extensions))
            {
                Console.WriteLine($"Pipeline configured with extensions: {pipeline.Key}");
                TestSpec(inputText, expectedOutputText, pipeline.Value);
            }
        }

        public static void TestSpec(string inputText, string expectedOutputText, MarkdownPipeline pipeline)
        {
            // Uncomment this line to get more debug information for process inlines.
            //pipeline.DebugLog = Console.Out;
            var result = Markdown.ToHtml(inputText, pipeline);

            result = Compact(result);
            expectedOutputText = Compact(expectedOutputText);

            Console.WriteLine("```````````````````Source");
            Console.WriteLine(DisplaySpaceAndTabs(inputText));
            Console.WriteLine("```````````````````Result");
            Console.WriteLine(DisplaySpaceAndTabs(result));
            Console.WriteLine("```````````````````Expected");
            Console.WriteLine(DisplaySpaceAndTabs(expectedOutputText));
            Console.WriteLine("```````````````````");
            Console.WriteLine();
            TextAssert.AreEqual(expectedOutputText, result);
        }

        private static IEnumerable<KeyValuePair<string, MarkdownPipeline>> GetPipeline(string extensionsGroupText)
        {
            // For the standard case, we make sure that both the CommmonMark core and Extra/Advanced are CommonMark compliant!
            if (string.IsNullOrEmpty(extensionsGroupText))
            {
                yield return new KeyValuePair<string, MarkdownPipeline>("default", new MarkdownPipelineBuilder().Build());

                yield return new KeyValuePair<string, MarkdownPipeline>("advanced", new MarkdownPipelineBuilder()  // Use similar to advanced extension without auto-identifier
                 .UseAbbreviations()
                //.UseAutoIdentifiers()
                .UseCitations()
                .UseCustomContainers()
                .UseDefinitionLists()
                .UseEmphasisExtras()
                .UseFigures()
                .UseFooters()
                .UseFootnotes()
                .UseGridTables()
                .UseMathematics()
                .UseMediaLinks()
                .UsePipeTables()
                .UseListExtras()
                .UseGenericAttributes().Build());

                yield break;
            }

            var extensionGroups = extensionsGroupText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var extensionsText in extensionGroups)
            {
                var builder = new MarkdownPipelineBuilder();
                builder.DebugLog = Console.Out;
                if (extensionsText == "jiralinks")
                {
                    builder.UseJiraLinks(new JiraLinkOptions("http://your.company.abc"));
                }
                else
                {
                    builder = extensionsText == "self" ? builder.UseSelfPipeline() : builder.Configure(extensionsText);
                }
                yield return new KeyValuePair<string, MarkdownPipeline>(extensionsText, builder.Build());
            }
        }

        public static string DisplaySpaceAndTabs(string text)
        {
            // Output special characters to check correctly the results
            return text.Replace('\t', '→').Replace(' ', '·');
        }

        private static string Compact(string html)
        {
            // Normalize the output to make it compatible with CommonMark specs
            html = html.Replace("\r\n", "\n").Replace(@"\r", @"\n").Trim();
            html = Regex.Replace(html, @"\s+</li>", "</li>");
            html = Regex.Replace(html, @"<li>\s+", "<li>");
            html = html.Normalize(NormalizationForm.FormKD);
            return html;
        }
    }
}