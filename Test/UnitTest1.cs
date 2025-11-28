using System.Text.RegularExpressions;

namespace Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Regex tokenPattern = new Regex(@"(?:(\d+)x)?(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            tokenPattern.Match("2xAlienHead");
            string input = "2xAlienHead, 2xArmorPiercing, 2x{ Random, { Cool, Bool } }, { Alien }";
            //StartBonusMod.Helper.ParseItemStringReward(input);
        }
    }
}