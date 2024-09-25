using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace NugetTestLocal
{
    /// <summary>
    /// The definition of a metrics group
    /// </summary>
    [MetricFactory(targetNamespace: "IC3TestService", targetAccount: "testAccount")]
    internal sealed partial class ServiceCustomMetric
    {
        // define custom dimensions here
        public const string CustomDimension1 = "custom.dimension_a";

        public const string CustomDimension2 = "custom.dimension_b";

        /// <summary>
        /// The Histogram metric for CustomMetric
        /// </summary>
        [Histogram(CustomDimension1, CustomDimension2, Name = "my.custom_metric_a")]
        public static partial CustomMetric CreateCustomMetric(Meter meter);


        /// <summary>
        /// The Counter metric for CustomMetric2
        /// </summary>
        [Counter(CustomDimension1, Name = "my.custom_metric_b")]
        public static partial CustomMetric2 CreateCustomMetric2(Meter meter);
    }
}
