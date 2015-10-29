﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.TV
{
    public class TVSeriesManager : ITVSeriesManager
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        public TVSeriesManager(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var parentIds = string.IsNullOrEmpty(request.ParentId)
                ? new string[] { }
                : new[] { request.ParentId };

            var items = GetAllLibraryItems(user, parentIds, i => i is Series)
                .Cast<Series>();

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items);

            return GetResult(episodes, null, request);
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request, IEnumerable<Folder> parentsFolders)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var items = parentsFolders
                .SelectMany(i => i.GetRecursiveChildren(user, s => s is Series))
                .Cast<Series>();

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items);

            return GetResult(episodes, null, request);
        }

        private IEnumerable<BaseItem> GetAllLibraryItems(User user, string[] parentIds, Func<BaseItem,bool> filter)
        {
            if (parentIds.Length > 0)
            {
                return parentIds.SelectMany(i =>
                {
                    var folder = (Folder)_libraryManager.GetItemById(new Guid(i));

                    return folder.GetRecursiveChildren(user, filter);

                });
            }

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            return user.RootFolder.GetRecursiveChildren(user, filter);
        }

        public IEnumerable<Episode> GetNextUpEpisodes(NextUpQuery request, User user, IEnumerable<Series> series)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            return FilterSeries(request, series)
                .AsParallel()
                .Select(i => GetNextUp(i, currentUser))
                // Include if an episode was found, and either the series is not unwatched or the specific series was requested
                .Where(i => i.Item1 != null && (!i.Item3 || !string.IsNullOrWhiteSpace(request.SeriesId)))
                .OrderByDescending(i =>
                {
                    var episode = i.Item1;

                    var seriesUserData = _userDataManager.GetUserData(user.Id, episode.Series.GetUserDataKey());

                    if (seriesUserData.IsFavorite)
                    {
                        return 2;
                    }

                    if (seriesUserData.Likes.HasValue)
                    {
                        return seriesUserData.Likes.Value ? 1 : -1;
                    }

                    return 0;
                })
                .ThenByDescending(i => i.Item2)
                .ThenByDescending(i => i.Item1.PremiereDate ?? DateTime.MinValue)
                .Select(i => i.Item1);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{Episode}.</returns>
        private Tuple<Episode, DateTime, bool> GetNextUp(Series series, User user)
        {
            // Get them in display order, then reverse
            var allEpisodes = series.GetSeasons(user, true, true)
                .Where(i => !i.IndexNumber.HasValue || i.IndexNumber.Value != 0)
                .SelectMany(i => i.GetEpisodes(user))
                .Reverse()
                .ToList();

            Episode lastWatched = null;
            var lastWatchedDate = DateTime.MinValue;
            Episode nextUp = null;

            var includeMissing = user.Configuration.DisplayMissingEpisodes;

            // Go back starting with the most recent episodes
            foreach (var episode in allEpisodes)
            {
                var userData = _userDataManager.GetUserData(user.Id, episode.GetUserDataKey());

                if (userData.Played)
                {
                    if (lastWatched != null || nextUp == null)
                    {
                        break;
                    }

                    lastWatched = episode;
                    lastWatchedDate = userData.LastPlayedDate ?? DateTime.MinValue;
                }
                else
                {
                    if (!episode.IsVirtualUnaired && (!episode.IsMissingEpisode || includeMissing))
                    {
                        nextUp = episode;
                    }
                }
            }

            if (lastWatched != null)
            {
                return new Tuple<Episode, DateTime, bool>(nextUp, lastWatchedDate, false);
            }

            var firstEpisode = allEpisodes.LastOrDefault(i => !i.IsVirtualUnaired && (!i.IsMissingEpisode || includeMissing) && !i.IsPlayed(user));

            // Return the first episode
            return new Tuple<Episode, DateTime, bool>(firstEpisode, DateTime.MinValue, true);
        }

        private IEnumerable<Series> FilterSeries(NextUpQuery request, IEnumerable<Series> items)
        {
            if (!string.IsNullOrWhiteSpace(request.SeriesId))
            {
                var id = new Guid(request.SeriesId);

                items = items.Where(i => i.Id == id);
            }

            return items;
        }

        private QueryResult<BaseItem> GetResult(IEnumerable<BaseItem> items, int? totalRecordLimit, NextUpQuery query)
        {
            var itemsArray = totalRecordLimit.HasValue ? items.Take(totalRecordLimit.Value).ToArray() : items.ToArray();
            var totalCount = itemsArray.Length;

            if (query.Limit.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex ?? 0).Take(query.Limit.Value).ToArray();
            }
            else if (query.StartIndex.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex.Value).ToArray();
            }

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = itemsArray
            };
        }
    }
}
