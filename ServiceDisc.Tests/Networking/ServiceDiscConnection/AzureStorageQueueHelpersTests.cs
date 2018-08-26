using System;
using ServiceDisc.Networking.ServiceDiscConnection;
using Xunit;

namespace ServiceDisc.Tests.Networking.ServiceDiscConnection
{
    public class AzureStorageQueueHelpersTests
    {
        public class GetQueueNameMethod
        {
            [Fact]
            public void ThrowsExceptionWhenNull()
            {
                Assert.Throws<ArgumentNullException>(() => AzureStorageQueueHelpers.GetQueueName(null));
            }

            [Fact]
            public void CreateQueueNameFromQualifiedName()
            {
                var result = AzureStorageQueueHelpers.GetQueueName(typeof(int));

                Assert.Equal("system-int32", result);
            }

            [Fact]
            public void AddsSuffixForSmallQueueNames()
            {
                var result = AzureStorageQueueHelpers.GetQueueName(typeof(S));

                Assert.Equal("s-q", result);
            }

            /// <summary>
            /// Used to test queue name for type with too large qualified name
            /// </summary>
            struct LargeTypeWithANameThatIsTooLargeInsideANamespaceAndAClassAndABitMore
            {
            }

            [Fact]
            public void RemoveBeginningOfLargeQueueNames()
            {
                var result = AzureStorageQueueHelpers.GetQueueName(typeof(LargeTypeWithANameThatIsTooLargeInsideANamespaceAndAClassAndABitMore));

                Assert.Equal("typewithanamethatistoolargeinsideanamespaceandaclassandabitmore", result);
            }

            /// <summary>
            /// Used to test queue name trimming for type with leading dash
            /// </summary>
            struct TypeWithANameThatIsTooLargeInsideANamespace
            {
            }

            [Fact]
            public void TrimLeadingDashFromQueueName()
            {
                var result = AzureStorageQueueHelpers.GetQueueName(typeof(TypeWithANameThatIsTooLargeInsideANamespace));

                Assert.Equal("getqueuenamemethod-typewithanamethatistoolargeinsideanamespace", result);
            }
        }
    }
}

/// <summary>
/// Used to test queue name for type with small qualified name
/// </summary>
struct S
{
}