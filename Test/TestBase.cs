using Core;
using Core.Models;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Test.Models;

namespace Test
{
    public abstract class TestBase
    {
        protected abstract IQueryable<Person> PersonQuery { get; }
        protected abstract IQueryable<AdditionalPersonData> AdditionalDataQuery { get; }

        protected void ExecuteAndCheck(KeyValuePair<string, StringValues>[] oDataParams, IEnumerable<KeyValuePair<int, int?>> data)
        {
            ODataProvider<Person, AdditionalPersonData, int> oDataProvider =
                new ODataProvider<Person, AdditionalPersonData, int>(oDataParams,
                                                                   PersonQuery,
                                                                   AdditionalDataQuery,
                                                                   nameof(Person.Id),
                                                                   nameof(AdditionalPersonData.PersonId));
            QueryResponse<FullPerson> response = oDataProvider.Execute(buildFullPerson);
            CheckResponseData(response, data);
        }

        private Func<Person, AdditionalPersonData, FullPerson> buildFullPerson
        {
            get
            {
                return (Person person, AdditionalPersonData additionalData) =>
                {
                    return new FullPerson()
                    {
                        Id = person.Id,
                        Name = person.Name,
                        Surname = person.Surname,
                        BirthYear = additionalData?.BirthYear,
                        FavoriteColor = additionalData?.FavoriteColor,
                        Height = additionalData?.Height
                    };
                };
            }
        }

        private void CheckResponseData(QueryResponse<FullPerson> response, IEnumerable<KeyValuePair<int, int?>> data)
        {
            // Item number
            Assert.AreEqual(data.Count(), response.Results.Count(), "Incorrect result count");
            // Item data
            int cont = 0;
            foreach (KeyValuePair<int, int?> item in data)
            {
                Assert.AreEqual(PersonQuery.ElementAt(item.Key).Id, response.Results.ElementAt(cont).Id, "Id is not equal");
                Assert.AreEqual(PersonQuery.ElementAt(item.Key).Name, response.Results.ElementAt(cont).Name, "Name is not equal");
                Assert.AreEqual(PersonQuery.ElementAt(item.Key).Surname, response.Results.ElementAt(cont).Surname, "Surname is not equal");
                Assert.AreEqual(item.Value.HasValue ? 
                                        AdditionalDataQuery.ElementAt(item.Value.Value)?.BirthYear :
                                        null, response.Results.ElementAt(cont).BirthYear, "Birthyear is not equal");
                Assert.AreEqual(item.Value.HasValue ?
                                        AdditionalDataQuery.ElementAt(item.Value.Value)?.FavoriteColor :
                                        null, response.Results.ElementAt(cont).FavoriteColor, "FavoariteColor is not equal");
                Assert.AreEqual(item.Value.HasValue ?
                                        AdditionalDataQuery.ElementAt(item.Value.Value)?.Height :
                                        null, response.Results.ElementAt(cont).Height, "Height is not equal");
                cont++;
            }
        }

        protected KeyValuePair<string, StringValues>[] BuildODataParams(string filter, bool count, int? top, int? skip, string orderBy)
        {
            List<KeyValuePair<string, StringValues>> odataParams = new List<KeyValuePair<string, StringValues>>();
            if (!string.IsNullOrEmpty(filter))
                odataParams.Add(new KeyValuePair<string, StringValues>("$filter", filter));
            if (count)
                odataParams.Add(new KeyValuePair<string, StringValues>("$count", "true"));
            if (top.HasValue)
                odataParams.Add(new KeyValuePair<string, StringValues>("$top", top.Value.ToString()));
            if (skip.HasValue)
                odataParams.Add(new KeyValuePair<string, StringValues>("$skip", skip.Value.ToString()));
            if (!string.IsNullOrEmpty(orderBy))
                odataParams.Add(new KeyValuePair<string, StringValues>("$orderby", orderBy));
            return odataParams.ToArray();
        }
    }
}
