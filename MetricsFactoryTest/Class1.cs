using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.IC3.R9.Generators.MetricsGenerators
{
    /// <summary>
    /// This is to generate metric factory code, using source generator
    /// </summary>
    [Generator]
    public class MetricFactoryGenerator : IIncrementalGenerator
    {
        private static readonly List<string> supportedAttributes = new List<string> { "Histogram", "Counter", "Gauge", "HistogramAttribute", "CounterAttribute", "GaugeAttribute" };


        /// <summary>
        /// Generate specific metric factory function.
        /// </summary>
        private string GenerateFactoryMethod(MethodDeclarationSyntax method, string csharpNamespace, string className)
        {
            var methodName = method.Identifier.Text;
            var returnType = method.ReturnType.ToString();

            return $@"
        public global::{csharpNamespace}.{returnType} {methodName}()
        {{
            return global::{csharpNamespace}.{className}.{methodName}(this.meter);
        }}";
        }

        /// <summary>
        /// Initialize the mtric instance.
        /// </summary>
        private string InitializeMetrics(MethodDeclarationSyntax method, string csharpNamespace, string className)
        {
            var methodName = method.Identifier.Text;
            var returnType = method.ReturnType.ToString();
            string metricName = this.MetricNameLowerCase(returnType);

            return $@"
            this.{metricName} = global::{csharpNamespace}.{className}.{methodName}(this.meter);
            ";
        }

        /// <summary>
        /// Generate specific metric definition.
        /// </summary>
        private string GenerateMetricsDefinition(MethodDeclarationSyntax method, string csharpNamespace)
        {
            var returnType = method.ReturnType.ToString();

            string metricName = this.MetricNameLowerCase(returnType);
            return $@"
        public global::{csharpNamespace}.{returnType} {metricName};
        ";
        }

        private string MetricNameLowerCase(string metricName)
        {
            return char.ToLower(metricName[0]) + metricName.Substring(1);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (context, _) => (ClassDeclarationSyntax)context.Node)
                .Where(static classDeclaration => classDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "MetricFactory"));

            context.RegisterSourceOutput(classDeclarations, (spc, classDeclaration) =>
            {
                var attributes = classDeclaration.AttributeLists.SelectMany(al => al.Attributes);
                var metricInfoAttribute = attributes.FirstOrDefault(a => a.Name.ToString() == "MetricFactory");

                if (metricInfoAttribute != null)
                {
                    var metricNamespace = metricInfoAttribute.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
                    var metricAccount = metricInfoAttribute.ArgumentList.Arguments[1].Expression.ToString().Trim('"');

                    var namespaceDeclaration = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    var namespaceValue = namespaceDeclaration?.Name.ToString();

                    var className = classDeclaration.Identifier.Text;

                    var metricList = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                        .Where(m => m.AttributeLists.SelectMany(al => al.Attributes).Any(a => supportedAttributes.Contains(a.Name.ToString())));

                    var metricDefinition = metricList
                        .Select(m => GenerateMetricsDefinition(m, namespaceValue));

                    var metricInits = metricList
                        .Select(m => InitializeMetrics(m, namespaceValue, className));

                    var methods = metricList
                        .Select(m => GenerateFactoryMethod(m, namespaceValue, className));

                    string source = $@"
using System.Diagnostics.Metrics;

namespace {namespaceValue}
{{
    internal sealed class {className}Factory
    {{
        private readonly Meter meter;
        {string.Join(string.Empty, metricDefinition)}
        public {className}Factory()
        {{
            MeterOptions meterOptions = new MeterOptions(""{className}"");

            if (meterOptions.Tags == null)
            {{
                meterOptions.Tags = new List<KeyValuePair<string, object>>();
            }}

            var tagList = meterOptions.Tags as List<KeyValuePair<string, object>>;
            tagList.Add(new KeyValuePair<string, object>(""_microsoft_metrics_namespace"", ""{metricNamespace}""));
            tagList.Add(new KeyValuePair<string, object>(""_microsoft_metrics_account"", ""{metricAccount}""));

            meterOptions.Tags = tagList;

            this.meter = new Meter(meterOptions);
            {string.Join(string.Empty, metricInits)}
        }}
    }}
}}
";

                    spc.AddSource($"{className}Factory.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            });
        }
    }
}
