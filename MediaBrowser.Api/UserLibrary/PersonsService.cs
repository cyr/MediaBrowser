﻿using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetPersons
    /// </summary>
    [Route("/Persons", "GET", Summary = "Gets all persons from a given item, folder, or the entire library")]
    public class GetPersons : GetItemsByName
    {
    }

    /// <summary>
    /// Class GetPerson
    /// </summary>
    [Route("/Persons/{Name}", "GET", Summary = "Gets a person, by name")]
    public class GetPerson : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The person name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class PersonsService
    /// </summary>
    [Authenticated]
    public class PersonsService : BaseItemsByNameService<Person>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepo">The item repo.</param>
        public PersonsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPerson request)
        {
            var result = GetItem(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetPerson request)
        {
            var item = GetPerson(request.Name, LibraryManager);

            var dtoOptions = GetDtoOptions(request);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = UserManager.GetUserById(request.UserId);

                return DtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return DtoService.GetBaseItemDto(item, dtoOptions);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersons request)
        {
            var result = GetResult(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var inputPersonTypes = ((GetPersons)request).PersonTypes;
            var personTypes = string.IsNullOrEmpty(inputPersonTypes) ? new string[] { } : inputPersonTypes.Split(',');

            var itemsList = items.ToList();

            // Either get all people, or all people filtered by a specific person type
            var allPeople = GetAllPeople(itemsList, personTypes);

            return allPeople
                .Select(i => i.Name)
                .DistinctNames()

                .Select(name =>
                {
                    try
                    {
                        return LibraryManager.GetPerson(name);
                    }
                    catch
                    {
                        return null;
                        // Already logged at lower levels
                    }
                }

            ).Where(i => i != null);
        }

        /// <summary>
        /// Gets all people.
        /// </summary>
        /// <param name="itemsList">The items list.</param>
        /// <param name="personTypes">The person types.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetAllPeople(IEnumerable<BaseItem> itemsList, IEnumerable<string> personTypes)
        {
            var allIds = itemsList.Select(i => i.Id).ToList();

            var allPeople = LibraryManager.GetPeople(new InternalPeopleQuery
            {
                PersonTypes = personTypes.ToList()
            });

            return allPeople.Where(i => allIds.Contains(i.ItemId)).OrderBy(p => p.SortOrder ?? int.MaxValue).ThenBy(p => p.Type);
        }
    }
}
