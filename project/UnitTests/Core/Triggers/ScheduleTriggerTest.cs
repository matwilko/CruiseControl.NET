using System;
using Exortech.NetReflector;
using NMock;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Core.Triggers;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.UnitTests.Core.Triggers
{
	[TestFixture]
	public class ScheduleTriggerTest
	{
		private IMock mockDateTime;
		private ScheduleTrigger trigger;

		[SetUp]
		public void Setup()
		{
			mockDateTime = new DynamicMock(typeof (DateTimeProvider));
			trigger = new ScheduleTrigger((DateTimeProvider) mockDateTime.MockInstance);
		}

		[TearDown]
		public void VerifyAll()
		{
			mockDateTime.Verify();
		}

		[Test]
		public void ShouldRunIntegrationIfCalendarTimeIsAfterIntegrationTime()
		{
			mockDateTime.SetupResult("Now", new DateTime(2004, 1, 1, 23, 25, 0, 0));
			trigger.Time = "23:30";
			trigger.BuildCondition = BuildCondition.IfModificationExists;

			Assert.AreEqual(BuildCondition.NoBuild, trigger.ShouldRunIntegration());

			mockDateTime.SetupResult("Now", new DateTime(2004, 1, 1, 23, 31, 0, 0));
			Assert.AreEqual(BuildCondition.IfModificationExists, trigger.ShouldRunIntegration());
		}

		[Test]
		public void ShouldRunIntegrationOnTheNextDay()
		{
			mockDateTime.SetupResult("Now", new DateTime(2004, 1, 1, 23, 25, 0, 0));
			trigger.Time = "23:30";
			trigger.BuildCondition = BuildCondition.IfModificationExists;

			mockDateTime.SetupResult("Now", new DateTime(2004, 1, 2, 1, 1, 0, 0));
			Assert.AreEqual(BuildCondition.IfModificationExists, trigger.ShouldRunIntegration());
		}

		[Test]
		public void ShouldIncrementTheIntegrationTimeToTheNextDayAfterIntegrationIsCompleted()
		{
			mockDateTime.SetupResult("Now", new DateTime(2004, 6, 27, 13, 00, 0, 0));
			trigger.Time = "14:30";
			trigger.BuildCondition = BuildCondition.IfModificationExists;
			mockDateTime.SetupResult("Now", new DateTime(2004, 6, 27, 15, 00, 0, 0));

			Assert.AreEqual(BuildCondition.IfModificationExists, trigger.ShouldRunIntegration());
			trigger.IntegrationCompleted();
			Assert.AreEqual(BuildCondition.NoBuild, trigger.ShouldRunIntegration());

			mockDateTime.SetupResult("Now", new DateTime(2004, 6, 28, 15, 00, 0, 0));
			Assert.AreEqual(BuildCondition.IfModificationExists, trigger.ShouldRunIntegration());
		}

		[Test]
		public void ShouldReturnSpecifiedBuildConditionWhenShouldRunIntegration()
		{
			foreach (BuildCondition expectedCondition in Enum.GetValues(typeof (BuildCondition)))
			{
				mockDateTime.SetupResult("Now", new DateTime(2004, 1, 1, 23, 25, 0, 0));
				trigger.Time = "23:30";
				trigger.BuildCondition = expectedCondition;
				Assert.AreEqual(BuildCondition.NoBuild, trigger.ShouldRunIntegration());

				mockDateTime.SetupResult("Now", new DateTime(2004, 1, 1, 23, 31, 0, 0));
				Assert.AreEqual(expectedCondition, trigger.ShouldRunIntegration());
			}
		}

		[Test]
		public void ShouldOnlyRunOnSpecifiedDays()
		{
			trigger.WeekDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday };
			trigger.BuildCondition = BuildCondition.ForceBuild;

			mockDateTime.SetupResult("Now", new DateTime(2004, 12, 1));
			Assert.AreEqual(BuildCondition.ForceBuild, trigger.ShouldRunIntegration());

			mockDateTime.SetupResult("Now", new DateTime(2004, 12, 2));
			Assert.AreEqual(BuildCondition.NoBuild, trigger.ShouldRunIntegration());
		}

		[Test]
		public void ShouldFullyPopulateFromReflector()
		{
			string xml = string.Format(@"<scheduleTrigger time=""12:00:00"" buildCondition=""ForceBuild"">
<weekDays>
	<weekDay>Monday</weekDay>
	<weekDay>Tuesday</weekDay>
</weekDays>
</scheduleTrigger>");
			trigger = (ScheduleTrigger)NetReflector.Read(xml);
			Assert.AreEqual("12:00:00", trigger.Time);
			Assert.AreEqual(DayOfWeek.Monday, trigger.WeekDays[0]);
			Assert.AreEqual(DayOfWeek.Tuesday, trigger.WeekDays[1]);
			Assert.AreEqual(BuildCondition.ForceBuild, trigger.BuildCondition);
		}

		[Test]
		public void ShouldMinimallyPopulateFromReflector()
		{
			string xml = string.Format(@"<scheduleTrigger time=""10:00:00"" />");
			trigger = (ScheduleTrigger)NetReflector.Read(xml);
			Assert.AreEqual("10:00:00", trigger.Time);
			Assert.AreEqual(7, trigger.WeekDays.Length);
			Assert.AreEqual(BuildCondition.IfModificationExists, trigger.BuildCondition);
		}
	}
}