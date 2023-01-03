﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EXBP.Dipren.Data.SqlServer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SqlServerEngineDataStoreImplementationResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SqlServerEngineDataStoreImplementationResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("EXBP.Dipren.Data.SqlServer.SqlServerEngineDataStoreImplementationResources", typeof(SqlServerEngineDataStoreImplementationResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        ///
        ///SELECT
        ///  (SELECT COUNT_BIG(1) FROM [dipren].[jobs] WHERE ([id] = @job_id)) AS [job_count],
        ///  (SELECT COUNT_BIG(1) FROM [dipren].[partitions] WHERE ([job_id] = @job_id) AND ([is_completed] = 0)) AS [partition_count];.
        /// </summary>
        internal static string QueryCountIncompletePartitions {
            get {
                return ResourceManager.GetString("QueryCountIncompletePartitions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        ///
        ///SELECT
        ///  COUNT_BIG(1) AS [count]
        ///FROM
        ///  [dipren].[jobs];.
        /// </summary>
        internal static string QueryCountJobs {
            get {
                return ResourceManager.GetString("QueryCountJobs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        ///
        ///SELECT
        ///  COUNT(1) AS [count]
        ///FROM
        ///  [dipren].[partitions]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryDoesPartitionExist {
            get {
                return ResourceManager.GetString("QueryDoesPartitionExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to INSERT INTO [dipren].[jobs]
        ///(
        ///  [id],
        ///  [created],
        ///  [updated],
        ///  [batch_size],
        ///  [timeout],
        ///  [clock_drift],
        ///  [started],
        ///  [completed],
        ///  [state],
        ///  [error]
        ///)
        ///VALUES
        ///(
        ///  @id,
        ///  @created,
        ///  @updated,
        ///  @batch_size,
        ///  @timeout,
        ///  @clock_drift,
        ///  @started,
        ///  @completed,
        ///  @state,
        ///  @error
        ///);.
        /// </summary>
        internal static string QueryInsertJob {
            get {
                return ResourceManager.GetString("QueryInsertJob", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to INSERT INTO [dipren].[partitions]
        ///(
        ///  [id],
        ///  [job_id],
        ///  [created],
        ///  [updated],
        ///  [owner],
        ///  [first],
        ///  [last],
        ///  [is_inclusive],
        ///  [position],
        ///  [processed],
        ///  [remaining],
        ///  [throughput],
        ///  [is_completed],
        ///  [split_requester]
        ///)
        ///VALUES
        ///(
        ///  @id,
        ///  @job_id,
        ///  @created,
        ///  @updated,
        ///  @owner,
        ///  @first,
        ///  @last,
        ///  @is_inclusive,
        ///  @position,
        ///  @processed,
        ///  @remaining,
        ///  @throughput,
        ///  @is_completed,
        ///  @split_requester
        ///);.
        /// </summary>
        internal static string QueryInsertPartition {
            get {
                return ResourceManager.GetString("QueryInsertPartition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
        ///
        ///SELECT
        ///  COALESCE((SELECT TOP (1) 1 FROM [dipren].[partitions] WHERE ([job_id] = @job_id) AND ([split_requester] = @requester)), 0) AS [requests_exist];.
        /// </summary>
        internal static string QueryIsSplitRequestPending {
            get {
                return ResourceManager.GetString("QueryIsSplitRequestPending", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[jobs]
        ///SET
        ///  [updated] = @timestamp,
        ///  [completed] = @timestamp,
        ///  [state] = @state
        ///OUTPUT
        ///  INSERTED.[id] AS [id],
        ///  INSERTED.[created] AS [created],
        ///  INSERTED.[updated] AS [updated],
        ///  INSERTED.[batch_size] AS [batch_size],
        ///  INSERTED.[timeout] AS [timeout],
        ///  INSERTED.[clock_drift] AS [clock_drift],
        ///  INSERTED.[started] AS [started],
        ///  INSERTED.[completed] AS [completed],
        ///  INSERTED.[state] AS [state],
        ///  INSERTED.[error] AS [error]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryMarkJobAsCompleted {
            get {
                return ResourceManager.GetString("QueryMarkJobAsCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[jobs]
        ///SET
        ///  [updated] = @timestamp,
        ///  [state] = @state,
        ///  [error] = @error
        ///OUTPUT
        ///  INSERTED.[id] AS [id],
        ///  INSERTED.[created] AS [created],
        ///  INSERTED.[updated] AS [updated],
        ///  INSERTED.[batch_size] AS [batch_size],
        ///  INSERTED.[timeout] AS [timeout],
        ///  INSERTED.[clock_drift] AS [clock_drift],
        ///  INSERTED.[started] AS [started],
        ///  INSERTED.[completed] AS [completed],
        ///  INSERTED.[state] AS [state],
        ///  INSERTED.[error] AS [error]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryMarkJobAsFailed {
            get {
                return ResourceManager.GetString("QueryMarkJobAsFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[jobs]
        ///SET
        ///  [updated] = @timestamp,
        ///  [state] = @state
        ///OUTPUT
        ///  INSERTED.[id] AS [id],
        ///  INSERTED.[created] AS [created],
        ///  INSERTED.[updated] AS [updated],
        ///  INSERTED.[batch_size] AS [batch_size],
        ///  INSERTED.[timeout] AS [timeout],
        ///  INSERTED.[clock_drift] AS [clock_drift],
        ///  INSERTED.[started] AS [started],
        ///  INSERTED.[completed] AS [completed],
        ///  INSERTED.[state] AS [state],
        ///  INSERTED.[error] AS [error]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryMarkJobAsReady {
            get {
                return ResourceManager.GetString("QueryMarkJobAsReady", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[jobs]
        ///SET
        ///  [updated] = @timestamp,
        ///  [started] = @timestamp,
        ///  [state] = @state
        ///OUTPUT
        ///  INSERTED.[id] AS [id],
        ///  INSERTED.[created] AS [created],
        ///  INSERTED.[updated] AS [updated],
        ///  INSERTED.[batch_size] AS [batch_size],
        ///  INSERTED.[timeout] AS [timeout],
        ///  INSERTED.[clock_drift] AS [clock_drift],
        ///  INSERTED.[started] AS [started],
        ///  INSERTED.[completed] AS [completed],
        ///  INSERTED.[state] AS [state],
        ///  INSERTED.[error] AS [error]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryMarkJobAsStarted {
            get {
                return ResourceManager.GetString("QueryMarkJobAsStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[partitions]
        ///SET
        ///  [updated] = @updated,
        ///  [position] = @position,
        ///  [processed] = @processed,
        ///  [remaining] = @remaining,
        ///  [throughput] = @throughput,
        ///  [is_completed] = @completed,
        ///  [split_requester] = CASE WHEN @completed = 1 THEN NULL ELSE [split_requester] END
        ///OUTPUT
        ///  INSERTED.[id] AS [id],
        ///  INSERTED.[job_id] AS [job_id],
        ///  INSERTED.[created] AS [created],
        ///  INSERTED.[updated] AS [updated],
        ///  INSERTED.[owner] AS [owner],
        ///  INSERTED.[first] AS [first],
        ///  INSERTED.[ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string QueryReportProgress {
            get {
                return ResourceManager.GetString("QueryReportProgress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        ///
        ///SELECT
        ///  [id] AS &quot;id&quot;,
        ///  [created] AS [created],
        ///  [updated] AS [updated],
        ///  [batch_size] AS [batch_size],
        ///  [timeout] AS [timeout],
        ///  [clock_drift] AS [clock_drift],
        ///  [started] AS [started],
        ///  [completed] AS [completed],
        ///  [state] AS [state],
        ///  [error] AS [error]
        ///FROM
        ///  [dipren].[jobs]
        ///WHERE
        ///  ([id] = @id);.
        /// </summary>
        internal static string QueryRetrieveJobById {
            get {
                return ResourceManager.GetString("QueryRetrieveJobById", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
        ///
        ///WITH &quot;aggregates&quot; AS
        ///(
        ///  SELECT
        ///    t1.[id] AS [id],
        ///    t1.[created] AS [created],
        ///    t1.[updated] AS [updated],
        ///    t1.[batch_size] AS [batch_size],
        ///    t1.[timeout] AS [timeout],
        ///    t1.[clock_drift] AS [clock_drift],
        ///    t1.[started] AS [started],
        ///    t1.[completed] AS [completed],
        ///    t1.[state] AS [state],
        ///    COUNT_BIG(CASE WHEN ((t2.[is_completed] = 0) AND (t2.[owner] IS NULL) AND (t2.[processed] = 0)) THEN 1 END) AS [partitons_untouc [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string QueryRetrieveJobStatusReport {
            get {
                return ResourceManager.GetString("QueryRetrieveJobStatusReport", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        ///
        ///SELECT
        ///  [id] AS [id],
        ///  [job_id] AS [job_id],
        ///  [created] AS [created],
        ///  [updated] AS [updated],
        ///  [owner] AS [owner],
        ///  [first] AS [first],
        ///  [last] AS [last],
        ///  [is_inclusive] AS [is_inclusive],
        ///  [position] AS [position],
        ///  [processed] AS [processed],
        ///  [remaining] AS [remaining],
        ///  [throughput] AS [throughput],
        ///  [is_completed] AS [is_completed],
        ///  [split_requester] AS [split_requester]
        ///FROM
        ///  [dipren].[partitions]
        ///WHERE
        ///  ([id] = @i [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string QueryRetrievePartitionById {
            get {
                return ResourceManager.GetString("QueryRetrievePartitionById", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT
        ///  1
        ///FROM
        ///  [dipren].[jobs] WITH (NOLOCK)
        ///WHERE
        ///  ([id] = @job_id);
        ///
        ///SELECT TOP (@candidates)
        ///  [id]
        ///INTO
        ///  [#candidates]
        ///FROM
        ///  [dipren].[partitions] WITH (NOLOCK)
        ///WHERE
        ///  ([job_id] = @job_id) AND
        ///  (([owner] IS NULL) OR ([updated] &lt; @active)) AND
        ///  ([is_completed] = 0)
        ///ORDER BY
        ///  [remaining] DESC;
        ///
        ///WITH [candidate] AS
        ///(
        ///  SELECT TOP 1
        ///    t2.[id]
        ///  FROM
        ///    [#candidates] AS t1
        ///    INNER JOIN [dipren].[partitions] AS t2 WITH (ROWLOCK, UPDLOCK) ON (t1.[id] = t2.[id])
        ///  WHERE [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string QueryTryAcquirePartition {
            get {
                return ResourceManager.GetString("QueryTryAcquirePartition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT
        ///  1
        ///FROM
        ///  [dipren].[jobs] WITH (NOLOCK)
        ///WHERE
        ///  ([id] = @job_id);
        ///
        ///SELECT
        ///  [id]
        ///INTO
        ///  [#candidates]
        ///FROM
        ///  [dipren].[partitions] WITH (NOLOCK)
        ///WHERE
        ///  ([job_id] = @job_id) AND
        ///  ([owner] IS NOT NULL) AND
        ///  ([updated] &gt;= @active) AND
        ///  ([is_completed] = 0) AND
        ///  ([split_requester] IS NULL)
        ///ORDER BY
        ///  [remaining] DESC;
        ///
        ///WITH [candidate] AS
        ///(
        ///  SELECT TOP 1
        ///    t2.[id]
        ///  FROM
        ///    [#candidates] AS t1
        ///    INNER JOIN [dipren].[partitions] AS t2 WITH (ROWLOCK, UPDLOCK) ON (t1. [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string QueryTryRequestSplit {
            get {
                return ResourceManager.GetString("QueryTryRequestSplit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to UPDATE
        ///  [dipren].[partitions]
        ///SET
        ///  [updated] = @updated,
        ///  [last] = @last,
        ///  [is_inclusive] = @is_inclusive,
        ///  [position] = @position,
        ///  [processed] = @processed,
        ///  [remaining] = @remaining,
        ///  [throughput] = @throughput,
        ///  [split_requester] = @split_requester
        ///WHERE
        ///  ([id] = @partition_id) AND
        ///  ([owner] = @owner);.
        /// </summary>
        internal static string QueryUpdateSplitPartition {
            get {
                return ResourceManager.GetString("QueryUpdateSplitPartition", resourceCulture);
            }
        }
    }
}
