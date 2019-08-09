using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AndroidBinderator;
using Xunit;

namespace Xamarin.AndroidBinderator.Tests
{
	public class BaseTest : IDisposable
	{
		public BaseTest()
		{
			RootDirectory = GetTempDirectory();
		}

		public string RootDirectory { get; }

		public void Dispose()
		{
			if (Directory.Exists(RootDirectory))
				Directory.Delete(RootDirectory, true);
		}

		public static string GetTempDirectory(bool createDirectory = true)
		{
			var newPath = Path.Combine(
				Path.GetTempPath(),
				"Xamarin.AndroidBinderator.Tests",
				Guid.NewGuid().ToString());

			if (createDirectory && !Directory.Exists(newPath))
				Directory.CreateDirectory(newPath);

			return newPath;
		}

		public string CreateTemplate(string filename, string contents)
		{
			var template = Path.Combine(RootDirectory, filename);
			File.WriteAllText(template, contents);
			return template;
		}

		public string CreateTemplate(string contents = null) =>
			CreateTemplate("Template.cshtml", contents ?? @"<Project Sdk=""Microsoft.NET.Sdk""></Project>");
	}

	public class GenerationTests : BaseTest
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public async Task ExternalsAreNotDownloadedOnlyWhenRequested(bool shouldDownload)
		{
			var externals = Path.Combine(RootDirectory, "externals");
			var generated = Path.Combine(RootDirectory, "generated");

			var config = new BindingConfig
			{
				DownloadExternals = shouldDownload,
				ExternalsDir = externals,
				BasePath = RootDirectory,
				Templates = { new TemplateConfig(CreateTemplate(), "generated/{artifactid}.csproj") },
				MavenArtifacts =
				{
					new MavenArtifactConfig
					{
						GroupId = "androidx.annotation",
						ArtifactId = "annotation",
						Version = "1.0.2",
						NugetPackageId = "Xamarin.AndroidX.Annotation",
					}
				}
			};

			await Engine.BinderateAsync(config);

			Assert.True(File.Exists(Path.Combine(generated, "annotation.csproj")));
			Assert.Equal(shouldDownload, Directory.Exists(externals));
			Assert.Equal(shouldDownload, File.Exists(Path.Combine(externals, "androidx.annotation", "annotation.jar")));
		}

		[Fact]
		public async Task TemplateIsCorrectlyGenerated()
		{
			var generated = Path.Combine(RootDirectory, "generated");

			var template = CreateTemplate(@"
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFrameworks>monoandroid9.0</TargetFrameworks>
		<AssemblyName>@(Model.NuGetPackageId)</AssemblyName>
	</PropertyGroup>
	<PropertyGroup>
		<PackageId>@(Model.NuGetPackageId)</PackageId>
		<Title>Xamarin Android Support Library - @(Model.Name)</Title>
		<PackageVersion>@(Model.NuGetVersion)</PackageVersion>
	</PropertyGroup>
</Project>");

			const string expected = @"
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFrameworks>monoandroid9.0</TargetFrameworks>
		<AssemblyName>Xamarin.AndroidX.Annotation</AssemblyName>
	</PropertyGroup>
	<PropertyGroup>
		<PackageId>Xamarin.AndroidX.Annotation</PackageId>
		<Title>Xamarin Android Support Library - annotation</Title>
		<PackageVersion>1.0.2</PackageVersion>
	</PropertyGroup>
</Project>";

			var config = new BindingConfig
			{
				DownloadExternals = false,
				BasePath = RootDirectory,
				Templates = { new TemplateConfig(template, "generated/{artifactid}.csproj") },
				MavenArtifacts =
				{
					new MavenArtifactConfig
					{
						GroupId = "androidx.annotation",
						ArtifactId = "annotation",
						Version = "1.0.2",
						NugetPackageId = "Xamarin.AndroidX.Annotation",
					}
				}
			};

			await Engine.BinderateAsync(config);

			var csproj = Path.Combine(generated, "annotation.csproj");

			Assert.True(File.Exists(csproj));
			Assert.Equal(expected, File.ReadAllText(csproj));
		}

		[Fact]
		public async Task NuGetVersionOverridesArtifactVersion()
		{
			var generated = Path.Combine(RootDirectory, "generated");

			var template = CreateTemplate(@"
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFrameworks>monoandroid9.0</TargetFrameworks>
		<AssemblyName>@(Model.NuGetPackageId)</AssemblyName>
	</PropertyGroup>
	<PropertyGroup>
		<PackageId>@(Model.NuGetPackageId)</PackageId>
		<Title>Xamarin Android Support Library - @(Model.Name)</Title>
		<PackageVersion>@(Model.NuGetVersion)</PackageVersion>
	</PropertyGroup>
</Project>");

			const string expected = @"
<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFrameworks>monoandroid9.0</TargetFrameworks>
		<AssemblyName>Xamarin.AndroidX.Annotation</AssemblyName>
	</PropertyGroup>
	<PropertyGroup>
		<PackageId>Xamarin.AndroidX.Annotation</PackageId>
		<Title>Xamarin Android Support Library - annotation</Title>
		<PackageVersion>1.2.3</PackageVersion>
	</PropertyGroup>
</Project>";

			var config = new BindingConfig
			{
				DownloadExternals = false,
				BasePath = RootDirectory,
				Templates = { new TemplateConfig(template, "generated/{artifactid}.csproj") },
				MavenArtifacts =
				{
					new MavenArtifactConfig
					{
						GroupId = "androidx.annotation",
						ArtifactId = "annotation",
						Version = "1.0.2",
						NugetPackageId = "Xamarin.AndroidX.Annotation",
						NugetVersion = "1.2.3",
					}
				}
			};

			await Engine.BinderateAsync(config);

			var csproj = Path.Combine(generated, "annotation.csproj");

			Assert.True(File.Exists(csproj));
			Assert.Equal(expected, File.ReadAllText(csproj));
		}
	}
}
