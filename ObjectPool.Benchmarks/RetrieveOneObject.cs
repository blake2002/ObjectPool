﻿// File name: RetrieveOneObject.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2013-2016 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using System;
using System.Linq;

namespace CodeProject.ObjectPool.Benchmarks
{
    [Config(typeof(Config))]
    public class RetrieveOneObject
    {
        private readonly ObjectPool<MyResource> _objectPool = new ObjectPool<MyResource>(9, 21, () => new MyResource { Value = DateTime.UtcNow.ToString() });
        private readonly ParameterizedObjectPool<int, MyResource> _paramObjectPool = new ParameterizedObjectPool<int, MyResource>(9, 21, x => new MyResource { Value = (DateTime.UtcNow + "#" + x) });
        private readonly Microsoft.Extensions.ObjectPool.ObjectPool<MyResource> _microsoftObjectPool = new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider().Create<MyResource>(new MyResource.Policy());

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default);
                Add(GetColumns().ToArray());
                Add(CsvExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub, PlainExporter.Default);
                Add(new MemoryDiagnoser());
                Add(EnvironmentAnalyser.Default);
            }
        }

        private sealed class MyResource : PooledObject
        {
            public string Value { get; set; }

            public sealed class Policy : Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<MyResource>
            {
#pragma warning disable CC0022 // Should dispose object
                public MyResource Create() => new MyResource { Value = DateTime.UtcNow.ToString() };
#pragma warning restore CC0022 // Should dispose object

                public bool Return(MyResource obj) => true;
            }
        }

        [Benchmark]
        public string SimpleObjectPool()
        {
            string str;
            using (var x = _objectPool.GetObject())
            {
                str = x.Value;
            }
            return str;
        }

        [Benchmark]
        public string ParameterizedObjectPool()
        {
            string str;
            using (var x = _paramObjectPool.GetObject(21))
            {
                str = x.Value;
            }
            return str;
        }

        [Benchmark]
        public string MicrosoftObjectPool()
        {
            MyResource res = null;
            string str;
            try
            {
                res = _microsoftObjectPool.Get();
                str = res.Value;
            }
            finally
            {
                if (res != null)
                {
                    _microsoftObjectPool.Return(res);
                }
            }
            return str;
        }
    }
}