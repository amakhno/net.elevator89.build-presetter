using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Elevator89.BuildPresetter.Tests
{
	public class UtilTests
	{
		[Test]
		public void HideAndRevealAssetTest()
		{
			string asset = "StreamingAssets/Configs/appsettings.json";

			Util.DisableAsset(asset);

			IEnumerable<string> hiddenAssets = Util.FindAllDisabledAssets();

			Assert.AreEqual(1, hiddenAssets.Count());
			Assert.True(Util.IsAssetDisabled(asset));

			Util.EnableAsset(asset);
			hiddenAssets = Util.FindAllDisabledAssets();
			Assert.AreEqual(0, hiddenAssets.Count());
			Assert.False(Util.IsAssetDisabled(asset));
		}
	}
}
