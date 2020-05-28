using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Test.Models;

namespace Test
{
    [TestClass]
    public class ComplementaryTest : TestBase
    {
        protected override IQueryable<Person> PersonQuery
        {
            get
            {
                return new Person[]
                {
                    new Person() { Id = 0, Name = "Peter", Surname="Brown"},
                    new Person() { Id = 1, Name = "Grace", Surname="Perry"},
                    new Person() { Id = 2, Name = "Anderson", Surname="Mathews"},
                    new Person() { Id = 3, Name = "Foster", Surname="Bramson"},
                    new Person() { Id = 4, Name = "Harris", Surname="Cook"},
                    new Person() { Id = 5, Name = "Anderson", Surname="Derricks"},
                    new Person() { Id = 6, Name = "Bill", Surname="Kane"},
                    new Person() { Id = 7, Name = "Bill", Surname="Mark"}
                }.AsQueryable();
            }
        }

        protected override IQueryable<AdditionalPersonData> AdditionalDataQuery
        {
            get
            {
                return new AdditionalPersonData[]
                {
                    new AdditionalPersonData() { Id = 0, PersonId = 0, BirthYear = 1980, FavoriteColor = "Green", Height = 1.8F },
                    new AdditionalPersonData() { Id = 1, PersonId = 2, BirthYear = 1978, FavoriteColor = "Blue", Height = 1.75F },
                    new AdditionalPersonData() { Id = 2, PersonId = 3, BirthYear = 1982, FavoriteColor = "Red", Height = 1.76F },
                    new AdditionalPersonData() { Id = 3, PersonId = 5, BirthYear = 1981, FavoriteColor = "Red", Height = 1.68F },
                    new AdditionalPersonData() { Id = 4, PersonId = 6, BirthYear = 1985, FavoriteColor = "Green", Height = 1.69F },
                    new AdditionalPersonData() { Id = 5, PersonId = 99, BirthYear = 1985, FavoriteColor = "Yellow", Height = 1.65F }
                }.AsQueryable();
            }
        }

        [TestMethod]
        public void EqualsFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear eq 1980",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0)
            });
        }

        [TestMethod]
        public void NotEqualsFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear ne 1985",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(5,3),
            });
        }

        [TestMethod]
        public void GreaterThanFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "Height gt 1,7",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void GreaterOrEqualThanFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear ge 1980",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void LessThanFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear lt 2000",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void LessOrEqualThanFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear le 1978",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(2,1),
            });
        }

        [TestMethod]
        public void ContainsFullDataFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "contains(FavoriteColor, 'Gre')",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void ComplexFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "contains(FavoriteColor, 'Green') or BirthYear eq '1981' or BirthYear eq 1985",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void AndFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "contains(FavoriteColor, 'Green') and BirthYear eq '1985'",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void OrFilter()
        {
            ExecuteAndCheck(BuildODataParams(
                            "contains(FavoriteColor, 'Red') or Height eq 1,75",
                            false,
                            null,
                            null,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(5,3),
            });
        }

        [TestMethod]
        public void BasicOrderByAscImplicit()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            null,
                            null,
                            "BirthYear"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(1,null),
                new KeyValuePair<int, int?>(4,null),
                new KeyValuePair<int, int?>(7,null),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void BasicOrderByAscExplicit()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            null,
                            null,
                            "BirthYear asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(1,null),
                new KeyValuePair<int, int?>(4,null),
                new KeyValuePair<int, int?>(7,null),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void BasicOrderByDesc()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            null,
                            null,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(1,null),
                new KeyValuePair<int, int?>(4,null),
                new KeyValuePair<int, int?>(7,null),
            });
        }

        [TestMethod]
        public void OrderByTop()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            6,
                            null,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(0,0),
                new KeyValuePair<int, int?>(3,2),
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(1,null),
            });
        }

        [TestMethod]
        public void OrderBySkip()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            null,
                            2,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(2,1),
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(1,null),
                new KeyValuePair<int, int?>(4,null),
                new KeyValuePair<int, int?>(7,null),
            });
        }

        [TestMethod]
        public void OrderByTopSkip()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            null,
                            false,
                            2,
                            3,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {                
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(5,3),             
            });
        }

        [TestMethod]
        public void OrderByTopSkipFilter()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            "contains(FavoriteColor, 'Red')",
                            false,
                            1,
                            1,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(5,3),
            });
        }

        [TestMethod]
        public void OrderByTopSkipFilterComplex()
        {
            KeyValuePair<string, StringValues>[] oDataParams = new KeyValuePair<string, StringValues>[]
            {
            };

            ExecuteAndCheck(BuildODataParams(
                            "BirthYear eq 1980 or contains(FavoriteColor, 'Red')",
                            false,
                            1,
                            1,
                            "Height desc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(3,2),
            });
        }
    }
}
