﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Wyam.Common.IO;
using Wyam.Core.IO;
using Wyam.Testing;

namespace Wyam.Core.Tests.IO
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class VirtualInputDirectoryTests : BaseFixture
    {
        public class ConstructorTests : VirtualInputDirectoryTests
        {
            [Test]
            public void ThrowsForNullFileSystem()
            {
                // Given, When, Then
                Assert.Throws<ArgumentNullException>(() => new VirtualInputDirectory(null, new DirectoryPath("A")));
            }

            [Test]
            public void ThrowsForNullDirectoryPath()
            {
                // Given, When, Then
                Assert.Throws<ArgumentNullException>(() => new VirtualInputDirectory(new FileSystem(), null));
            }

            [Test]
            public void ThrowsForNonRelativePath()
            {
                // Given, When, Then
                Assert.Throws<ArgumentException>(() => new VirtualInputDirectory(new FileSystem(), new DirectoryPath("/A")));
            }
        }

        public class GetDirectoriesTests : VirtualInputDirectoryTests
        {
            [Test]
            [TestCase(".", SearchOption.AllDirectories, new [] { "c", "c/1", "d", "a", "a/b" })]
            [TestCase(".", SearchOption.TopDirectoryOnly, new[] { "c", "d", "a" })]
            [TestCase("a", SearchOption.AllDirectories, new[] { "b" })]
            [TestCase("a", SearchOption.TopDirectoryOnly, new [] { "b" })]
            public void GetsDirectories(string virtualPath, SearchOption searchOption, string[] expectedPaths)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When
                IEnumerable<IDirectory> directories = directory.GetDirectories(searchOption);

                // Then
                CollectionAssert.AreEquivalent(expectedPaths, directories.Select(x => x.Path.FullPath));
            }
        }

        public class GetFilesTests : VirtualInputDirectoryTests
        {
            [Test]
            [TestCase(".", SearchOption.AllDirectories, new[] { "/a/b/c/foo.txt", "/a/b/c/1/2.txt", "/a/b/d/baz.txt", "/foo/baz.txt", "/foo/c/baz.txt" })]
            [TestCase(".", SearchOption.TopDirectoryOnly, new[] { "/foo/baz.txt" })]
            [TestCase("c", SearchOption.AllDirectories, new[] { "/a/b/c/foo.txt", "/a/b/c/1/2.txt", "/foo/c/baz.txt" })]
            [TestCase("c", SearchOption.TopDirectoryOnly, new [] { "/a/b/c/foo.txt", "/foo/c/baz.txt" })]
            public void GetsFiles(string virtualPath, SearchOption searchOption, string[] expectedPaths)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When
                IEnumerable<IFile> files = directory.GetFiles(searchOption);

                // Then
                CollectionAssert.AreEquivalent(expectedPaths, files.Select(x => x.Path.FullPath));
            }

        }

        public class GetFileTests : VirtualInputDirectoryTests
        {
            [Test]
            [TestCase(".", "c/foo.txt", "/a/b/c/foo.txt", true)]
            [TestCase(".", "baz.txt", "/foo/baz.txt", true)]
            [TestCase("c", "foo.txt", "/a/b/c/foo.txt", true)]
            [TestCase("c", "1/2.txt", "/a/b/c/1/2.txt", true)]
            [TestCase("c", "1/3.txt", "/foo/c/1/3.txt", false)]
            [TestCase("c", "baz.txt", "/foo/c/baz.txt", true)]
            [TestCase("c", "bar.txt", "/foo/c/bar.txt", false)]
            [TestCase("x/y/z", "bar.txt", "/foo/x/y/z/bar.txt", false)]
            public void GetsInputFile(string virtualPath, string filePath, string expectedPath, bool expectedExists)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When
                IFile file = directory.GetFile(filePath);

                // Then
                Assert.AreEqual(expectedPath, file.Path.FullPath);
                Assert.AreEqual(expectedExists, file.Exists);
            }
        }

        public class ParentPropertyTests : VirtualInputDirectoryTests
        {

            [Test]
            [TestCase("a/b", "a")]
            [TestCase("a/b/", "a")]
            [TestCase("a/b/../c", "a")]
            public void ReturnsParent(string virtualPath, string expected)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When
                IDirectory parent = directory.Parent;

                // Then
                Assert.AreEqual(expected, parent.Path.FullPath);
            }

            [TestCase(".")]
            [TestCase("a")]
            public void RootDirectoryReturnsNullParent(string virtualPath)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When
                IDirectory parent = directory.Parent;

                // Then
                Assert.IsNull(parent);
            }
        }

        public class ExistsPropertyTests : VirtualInputDirectoryTests
        {

            [Test]
            [TestCase(".")]
            [TestCase("c")]
            [TestCase("c/1")]
            [TestCase("a/b")]
            public void ShouldReturnTrueForExistingPaths(string virtualPath)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);
                
                // When, Then
                Assert.IsTrue(directory.Exists);
            }

            [TestCase("x")]
            [TestCase("bar")]
            [TestCase("baz")]
            [TestCase("a/b/c")]
            [TestCase("q/w/e")]
            public void ShouldReturnFalseForNonExistingPaths(string virtualPath)
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                fileSystem.RootPath = "/a";
                fileSystem.InputPaths.Add("b");
                fileSystem.InputPaths.Add("alt::/foo");
                fileSystem.FileProviders.Add(string.Empty, GetFileProviderA());
                fileSystem.FileProviders.Add("alt", GetFileProviderB());
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, virtualPath);

                // When, Then
                Assert.IsFalse(directory.Exists);
            }
        }

        public class CreateMethodTests : VirtualInputDirectoryTests
        {
            [Test]
            public void ShouldThrow()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, ".");

                // When, Then
                Assert.Throws<NotSupportedException>(() => directory.Create());
            }
        }

        public class DeleteMethodTests : VirtualInputDirectoryTests
        {
            [Test]
            public void ShouldThrow()
            {
                // Given
                FileSystem fileSystem = new FileSystem();
                VirtualInputDirectory directory = new VirtualInputDirectory(fileSystem, ".");

                // When, Then
                Assert.Throws<NotSupportedException>(() => directory.Delete(false));
            }
        }

        private IFileProvider GetFileProviderA()
        {
            string[] directories =
            {
                "/a",
                "/a/b",
                "/a/b/c",
                "/a/b/c/1",
                "/a/b/d",
                "/a/x",
                "/a/y",
                "/a/y/z"
            };
            string[] files =
            {
                "/a/b/c/foo.txt",
                "/a/b/c/baz.txt",
                "/a/b/c/1/2.txt",
                "/a/b/d/baz.txt",
                "/a/x/bar.txt"
            };
            IFileProvider fileProvider = Substitute.For<IFileProvider>();
            fileProvider.GetDirectory(Arg.Any<DirectoryPath>())
                .Returns(x =>
                {
                    string path = ((DirectoryPath)x[0]).FullPath;
                    return GetDirectory(path, directories.Contains(path), directories, files);
                });
            fileProvider.GetFile(Arg.Any<FilePath>())
                .Returns(x =>
                {
                    string path = ((FilePath)x[0]).FullPath;
                    return GetFile(path, files.Contains(path));
                });
            return fileProvider;
        }

        private IFileProvider GetFileProviderB()
        {
            string[] directories =
            {
                "/foo",
                "/foo/a",
                "/foo/a/b",
                "/foo/c",
                "/bar",
            };
            string[] files =
            {
                "/foo/baz.txt",
                "/foo/c/baz.txt",
                "/bar/baz.txt"
            };
            IFileProvider fileProvider = Substitute.For<IFileProvider>();
            fileProvider.GetDirectory(Arg.Any<DirectoryPath>())
                .Returns(x =>
                {
                    string path = ((DirectoryPath)x[0]).FullPath;
                    return GetDirectory(path, directories.Contains(path), directories, files);
                });
            fileProvider.GetFile(Arg.Any<FilePath>())
                .Returns(x =>
                {
                    string path = ((FilePath)x[0]).FullPath;
                    return GetFile(path, files.Contains(path));
                });
            return fileProvider;
        }

        private IFile GetFile(string path, bool exists)
        {
            IFile file = Substitute.For<IFile>();
            file.Path.Returns(new FilePath(path));
            file.Exists.Returns(exists);
            return file;
        }

        private IDirectory GetDirectory(string path, bool exists, string[] directories, string[] files)
        {
            IDirectory directory = Substitute.For<IDirectory>();
            directory.Path.Returns(new DirectoryPath(path));
            directory.GetDirectories(SearchOption.AllDirectories)
                .Returns(directories
                    .Where(x => x.StartsWith(path) && path != x)
                    .Select(x => GetDirectory(x, true, directories, files)));
            directory.GetDirectories(SearchOption.TopDirectoryOnly)
                .Returns(directories
                    .Where(x => x.StartsWith(path) && path.Count(c => c == '/') == x.Count(c => c == '/') - 1)
                    .Select(x => GetDirectory(x, true, directories, files)));
            directory.GetFiles(SearchOption.AllDirectories)
                .Returns(files
                    .Where(x => x.StartsWith(path))
                    .Select(x => GetFile(x, true)));
            directory.GetFiles(SearchOption.TopDirectoryOnly)
                .Returns(files
                    .Where(x => x.StartsWith(path) && path.Count(c => c == '/') == x.Count(c => c == '/') - 1)
                    .Select(x => GetFile(x, true)));
            directory.Exists.Returns(exists);
            return directory;
        }
    }
}
