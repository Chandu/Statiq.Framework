﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Statiq.Common;

namespace Statiq.Testing
{
    public class TestPipelineOutputs : IPipelineOutputs
    {
        public TestPipelineOutputs(IDictionary<string, ImmutableArray<IDocument>> outputs = null)
        {
            Dictionary = outputs ?? new Dictionary<string, ImmutableArray<IDocument>>();
        }

        public IDictionary<string, ImmutableArray<IDocument>> Dictionary { get; }

        public IReadOnlyDictionary<string, ImmutableArray<IDocument>> ByPipeline() =>
            Dictionary.ToDictionary(x => x.Key, x => x.Value);

        public IEnumerable<IDocument> ExceptPipeline(string pipelineName)
        {
            _ = pipelineName ?? throw new ArgumentNullException(nameof(pipelineName));
            return Dictionary
                .Where(x => !x.Key.Equals(pipelineName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.Value);
        }

        public ImmutableArray<IDocument> FromPipeline(string pipelineName)
        {
            _ = pipelineName ?? throw new ArgumentNullException(nameof(pipelineName));
            return Dictionary.TryGetValue(pipelineName, out ImmutableArray<IDocument> results)
                ? results
                : ImmutableArray<IDocument>.Empty;
        }

        public IEnumerator<IDocument> GetEnumerator() =>
            Dictionary.SelectMany(x => x.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
