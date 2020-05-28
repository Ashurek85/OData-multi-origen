using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Models;

namespace Test
{
    [TestClass]
    public class MixedTest : TestBase
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
        public void FilterByGuideOrderByComplementary()
        {
            ExecuteAndCheck(BuildODataParams(
                            "Name eq 'Bill'",
                            false,
                            null,
                            null,
                            "BirthYear asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(7,null),
                new KeyValuePair<int, int?>(6,4),                
            });
        }

        [TestMethod]
        public void FilterByGuideOrderByComplementaryTopSkip()
        {
            ExecuteAndCheck(BuildODataParams(
                            "Name eq 'Bill'",
                            false,
                            1,
                            1,
                            "BirthYear asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(6,4),
            });
        }

        [TestMethod]
        public void OrderByGuideFilterByComplementary()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980",
                            false,
                            null,
                            null,
                            "Name asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(5,3),
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void OrderByGuideFilterByComplementaryTopSkip()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980",
                            false,
                            2,
                            1,
                            "Name asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void OrderByGuideFilterByComplementaryAndGuide()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980 and Name eq 'Foster'",
                            false,
                            null,
                            null,
                            "Name asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void OrderByGuideFilterByComplementaryAndGuideTopSkip()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980 and Name eq 'Foster' or Name eq 'Bill' or Name eq 'Anderson'",
                            false,
                            2,
                            1,
                            "Name asc"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(6,4),
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void OrderByComplementaryFilterByComplementaryAndGuide()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980 and Name eq 'Foster'",
                            false,
                            null,
                            null,
                            "BirthYear"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void OrderByComplementaryFilterByComplementaryAndGuideTopSkip()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1980 and Name eq 'Foster' or Name eq 'Bill' or Name eq 'Anderson'",
                            false,
                            1,
                            1,
                            "BirthYear"),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(3,2),
            });
        }

        [TestMethod]
        public void FilterByComplementaryAndGuide()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1978 and Name eq 'Bill'",
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
        public void FilterByComplementaryAndGuideTopSkip()
        {
            ExecuteAndCheck(BuildODataParams(
                            "BirthYear gt 1978 and Name eq 'Anderson'",
                            false,
                            1,
                            0,
                            null),
                new KeyValuePair<int, int?>[]
            {
                new KeyValuePair<int, int?>(5,3),
            });
        }

        //ExecuteAndCheck(BuildODataParams(null, false, null, null, null),
        //    new KeyValuePair<int, int?>[]
        //{
        //    new KeyValuePair<int, int?>(0,0),
        //    new KeyValuePair<int, int?>(1,null),
        //    new KeyValuePair<int, int?>(2,1),
        //    new KeyValuePair<int, int?>(3,2),
        //    new KeyValuePair<int, int?>(4,null),
        //    new KeyValuePair<int, int?>(5,3),
        //    new KeyValuePair<int, int?>(6,4),
        //    new KeyValuePair<int, int?>(7,null)
        //});

    }
}
