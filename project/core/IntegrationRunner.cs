using System;
using System.IO;
using ThoughtWorks.CruiseControl.Core.Sourcecontrol;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.Core
{
	public class IntegrationRunner : IIntegratable
	{
		public IIntegrationRunnerTarget target;
		private readonly IIntegrationResultManager resultManager;
		private readonly IQuietPeriod quietPeriod;

		public IntegrationRunner(IIntegrationResultManager resultManager, IIntegrationRunnerTarget target,
		                         IQuietPeriod quietPeriod)
		{
			this.target = target;
			this.quietPeriod = quietPeriod;
			this.resultManager = resultManager;
		}

		public IIntegrationResult Integrate(IntegrationRequest request)
		{
			IIntegrationResult result = resultManager.StartNewIntegration(request);
			IIntegrationResult lastResult = resultManager.LastIntegrationResult;

			CreateDirectoryIfItDoesntExist(result.WorkingDirectory);
			CreateDirectoryIfItDoesntExist(result.ArtifactDirectory);
			result.MarkStartTime();
			try
			{
				result.Modifications = GetModifications(lastResult, result);
				if (result.ShouldRunBuild())
				{
					Log.Info("Building: " + request.ToString());
					target.Activity = ProjectActivity.Building;
					target.Prebuild(result);
					target.SourceControl.GetSource(result);
					target.Run(result);
					Log.Info("Build complete: " + result.Status);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				result.ExceptionResult = ex;
			}
			result.MarkEndTime();
			PostBuild(result);

			return result;
		}

		private Modification[] GetModifications(IIntegrationResult from, IIntegrationResult to)
		{
			target.Activity = ProjectActivity.CheckingModifications;
			return quietPeriod.GetModifications(target.SourceControl, from, to);
		}

		private void CreateDirectoryIfItDoesntExist(string directory)
		{
			if (! Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}

		private void PostBuild(IIntegrationResult result)
		{
			if (ShouldPublishResult(result))
			{
				LabelSourceControl(result);
				target.PublishResults(result);
				resultManager.FinishIntegration();
			}
			Log.Info("Integration complete: " + result.EndTime);

			target.Activity = ProjectActivity.Sleeping;
		}

		private void LabelSourceControl(IIntegrationResult result)
		{
			try
			{
				target.SourceControl.LabelSourceControl(result);
			}
			catch (Exception e)
			{
				Log.Error(new CruiseControlException("Exception occurred while labelling source control provider.", e));
			}
		}

		private bool ShouldPublishResult(IIntegrationResult result)
		{
			IntegrationStatus integrationStatus = result.Status;
			if (integrationStatus == IntegrationStatus.Exception)
			{
				return target.PublishExceptions;
			}
			else
			{
				return integrationStatus != IntegrationStatus.Unknown;
			}
		}
	}
}