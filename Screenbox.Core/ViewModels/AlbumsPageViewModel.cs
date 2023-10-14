﻿#nullable enable

using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Screenbox.Core.Helpers;
using Screenbox.Core.Messages;
using Screenbox.Core.Models;
using Screenbox.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Screenbox.Core.ViewModels
{
    public sealed partial class AlbumsPageViewModel : ObservableRecipient
    {
        public ObservableGroupedCollection<string, AlbumViewModel> GroupedAlbums { get; }

        private readonly ILibraryService _libraryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _refreshTimer;

        public AlbumsPageViewModel(ILibraryService libraryService)
        {
            _libraryService = libraryService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _refreshTimer = _dispatcherQueue.CreateTimer();
            GroupedAlbums = new ObservableGroupedCollection<string, AlbumViewModel>();
            PopulateGroups();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            _libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            _refreshTimer.Stop();
        }

        public void FetchAlbums()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = _libraryService.GetMusicFetchResult();

            IEnumerable<IGrouping<string, AlbumViewModel>> groupings = musicLibrary.Albums
                .OrderBy(a => a.Name, StringComparer.CurrentCulture)
                .GroupBy(album => album == musicLibrary.UnknownAlbum
                    ? "\u2026"
                    : MediaGroupingHelpers.GetFirstLetterGroup(album.Name));
            GroupedAlbums.SyncObservableGroups(groupings);

            // Progressively update when it's still loading
            if (_libraryService.IsLoadingMusic)
            {
                _refreshTimer.Debounce(FetchAlbums, TimeSpan.FromSeconds(5));
            }
            else
            {
                _refreshTimer.Stop();
            }
        }

        private void PopulateGroups()
        {
            foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
            {
                GroupedAlbums.AddGroup(key);
            }
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            _dispatcherQueue.TryEnqueue(FetchAlbums);
        }

        [RelayCommand]
        private void PlayAlbum(AlbumViewModel album)
        {
            if (album.RelatedSongs.Count == 0) return;
            MediaViewModel? inQueue = album.RelatedSongs.FirstOrDefault(m => m.IsMediaActive);
            if (inQueue != null)
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                List<MediaViewModel> songs = album.RelatedSongs
                    .OrderBy(m => m.TrackNumber)
                    .ThenBy(m => m.Name, StringComparer.CurrentCulture)
                    .ToList();

                Messenger.SendQueueAndPlay(inQueue ?? songs[0], songs);
            }
        }
    }
}
