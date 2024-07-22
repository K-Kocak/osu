﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osuTK;

namespace osu.Game.Screens.Ranking
{
    public partial class FavouriteButton : OsuAnimatedButton
    {
        private readonly Box background;
        private readonly SpriteIcon icon;

        public readonly BeatmapSetInfo BeatmapSetInfo;
        private APIBeatmapSet beatmapSet;
        private readonly Bindable<BeatmapSetFavouriteState> current;

        private PostBeatmapFavouriteRequest favouriteRequest;
        private readonly LoadingLayer loading;

        private readonly IBindable<APIUser> localUser = new Bindable<APIUser>();

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        public FavouriteButton(BeatmapSetInfo beatmapSetInfo)
        {
            BeatmapSetInfo = beatmapSetInfo;
            current = new BindableWithCurrent<BeatmapSetFavouriteState>(new BeatmapSetFavouriteState(false, 0));

            Size = new Vector2(50, 30);

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MaxValue
                },
                icon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(13),
                    Icon = FontAwesome.Regular.Heart,
                },
                loading = new LoadingLayer(true, false),
            };

            Action = toggleFavouriteStatus;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            current.BindValueChanged(_ => updateState(), true);

            localUser.BindTo(api.LocalUser);
            localUser.BindValueChanged(_ => updateUser(), true);
        }

        private void getBeatmapSet()
        {
            GetBeatmapSetRequest beatmapSetRequest = new GetBeatmapSetRequest(BeatmapSetInfo.OnlineID);

            loading.Show();
            beatmapSetRequest.Success += beatmapSet =>
            {
                this.beatmapSet = beatmapSet;
                current.Value = new BeatmapSetFavouriteState(this.beatmapSet.HasFavourited, this.beatmapSet.FavouriteCount);

                loading.Hide();
                Enabled.Value = true;
            };
            beatmapSetRequest.Failure += e =>
            {
                Logger.Error(e, $"Failed to fetch beatmap info: {e.Message}");

                loading.Hide();
                Enabled.Value = false;
            };
            api.Queue(beatmapSetRequest);
        }

        private void toggleFavouriteStatus()
        {
            Enabled.Value = false;
            loading.Show();

            var actionType = current.Value.Favourited ? BeatmapFavouriteAction.UnFavourite : BeatmapFavouriteAction.Favourite;

            favouriteRequest?.Cancel();
            favouriteRequest = new PostBeatmapFavouriteRequest(beatmapSet.OnlineID, actionType);

            favouriteRequest.Success += () =>
            {
                bool favourited = actionType == BeatmapFavouriteAction.Favourite;

                current.Value = new BeatmapSetFavouriteState(favourited, current.Value.FavouriteCount + (favourited ? 1 : -1));

                Enabled.Value = true;
                loading.Hide();
            };
            favouriteRequest.Failure += e =>
            {
                Logger.Error(e, $"Failed to {actionType.ToString().ToLowerInvariant()} beatmap: {e.Message}");
                Enabled.Value = true;
                loading.Hide();
            };

            api.Queue(favouriteRequest);
        }

        private void updateUser()
        {
            if (!(localUser.Value is GuestUser) && BeatmapSetInfo.OnlineID > 0)
                getBeatmapSet();
            else
            {
                Enabled.Value = false;
                current.Value = new BeatmapSetFavouriteState(false, 0);
                updateState();
                TooltipText = BeatmapsetsStrings.ShowDetailsFavouriteLogin;
            }
        }

        private void updateState()
        {
            if (current?.Value == null)
                return;

            if (current.Value.Favourited)
            {
                background.Colour = colours.Green;
                icon.Icon = FontAwesome.Solid.Heart;
                TooltipText = BeatmapsetsStrings.ShowDetailsUnfavourite;
            }
            else
            {
                background.Colour = colours.Gray4;
                icon.Icon = FontAwesome.Regular.Heart;
                TooltipText = BeatmapsetsStrings.ShowDetailsFavourite;
            }
        }
    }
}
