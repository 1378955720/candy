using Xunit;
using System.Collections;
using System.Linq;
using Candy.Model;
using Candy.SqlBuilder.ExpressionAnalysis;
namespace Candy.SqlExpression.XUnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var test = new TestModel() { Age = 1 };
            var name = "null";
            string sql8 = SqlGenerator.GetSelector<Users>(a => a.Age.Value, DataBaseType.PostgreSql);
            string sql7 = SqlGenerator.GetWhereByLambda<Users>(a => a.Age.Value != test.Age.Value && a.Id > 2, DataBaseType.PostgreSql);
            string sql5 = SqlGenerator.GetWhereByLambda<Users>(a => a.Name != null && a.Id > 2, DataBaseType.PostgreSql);
            string sql6 = SqlGenerator.GetWhereByLambda<Users>(a => a.Name != name && a.Id > 2, DataBaseType.PostgreSql);

            string sql1 = SqlGenerator.GetWhereByLambda<Users>(x => x.Name.StartsWith("test") && 2 > x.Id, DataBaseType.PostgreSql);
            string sql2 = SqlGenerator.GetWhereByLambda<Users>(x => x.Name.EndsWith("test") && (x.Id > 4 || x.Id == 3), DataBaseType.PostgreSql);
            string sql3 = SqlGenerator.GetWhereByLambda<Users>(x => x.Name.Contains("test") && (x.Id > 4 && x.Id <= 8), DataBaseType.PostgreSql);
            string sql4 = SqlGenerator.GetWhereByLambda<Users>(x => x.Name == "FengCode" && (x.Id >= 1), DataBaseType.PostgreSql);
        }
        public class TestModel
        {
            public int? Age { get; set; }
        }
    }
}
