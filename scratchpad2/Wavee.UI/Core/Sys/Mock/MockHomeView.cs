using System.Text.Json;
using Wavee.UI.Core.Contracts.Common;
using Wavee.UI.Core.Contracts.Home;

namespace Wavee.UI.Core.Sys.Mock;

internal sealed class MockHomeView : IHomeView
{
    private const string sample_json = """
                {
          "content" : {
            "href" : "https://api.spotify.com/v1/views/desktop-home?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=20&offset=0",
            "items" : [ {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/recently-played?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "href" : "https://api.spotify.com/v1/me/tracks",
                  "images" : [ {
                    "height" : 300,
                    "name" : "DEFAULT",
                    "url" : "https://misc.scdn.co/liked-songs/liked-songs-300.png",
                    "width" : 300
                  }, {
                    "height" : 640,
                    "name" : "LARGE",
                    "url" : "https://misc.scdn.co/liked-songs/liked-songs-640.png",
                    "width" : 640
                  } ],
                  "name" : "Liked Songs",
                  "type" : "link",
                  "uri" : "spotify:user:5fc6rpj232xgrtzzw4q3n790j:collection"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E8OeLm3ojuH7V"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E8OeLm3ojuH7V",
                  "id" : "37i9dQZF1E8OeLm3ojuH7V",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/track/02SbQgZbzMoylPoGr32ugF/en",
                    "width" : null
                  } ],
                  "name" : "Drama Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDliOTFjOThkOGNkMDI1NTM0ZTZlM2UxMmRjM2U3ZDhj",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E8OeLm3ojuH7V/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E8OeLm3ojuH7V"
                }, {
                  "collaborative" : false,
                  "description" : "Simply rain ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8ymr6UES7vc"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8ymr6UES7vc",
                  "id" : "37i9dQZF1DX8ymr6UES7vc",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000024a46a7f4e55bbc386dc77f84",
                    "width" : null
                  } ],
                  "name" : "Rain Sounds",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3NzUwODk2MiwwMDAwMDAwMDA4NTJkODI3MGQ5YWJkMjUyZTJhMWNmNDkzNWNkMGFl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8ymr6UES7vc/tracks",
                    "total" : 290
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8ymr6UES7vc"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/7jVv8c5Fj3E9VhNjxT4snq"
                    },
                    "href" : "https://api.spotify.com/v1/artists/7jVv8c5Fj3E9VhNjxT4snq",
                    "id" : "7jVv8c5Fj3E9VhNjxT4snq",
                    "name" : "Lil Nas X",
                    "type" : "artist",
                    "uri" : "spotify:artist:7jVv8c5Fj3E9VhNjxT4snq"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/0aIy6J8M9yHTnjtRu81Nr9"
                  },
                  "href" : "https://api.spotify.com/v1/albums/0aIy6J8M9yHTnjtRu81Nr9",
                  "id" : "0aIy6J8M9yHTnjtRu81Nr9",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27304cd9a1664fb4539a55643fe",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0204cd9a1664fb4539a55643fe",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485104cd9a1664fb4539a55643fe",
                    "width" : 64
                  } ],
                  "name" : "STAR WALKIN' (League of Legends Worlds Anthem)",
                  "release_date" : "2022-09-22",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:0aIy6J8M9yHTnjtRu81Nr9"
                }, {
                  "collaborative" : false,
                  "description" : "This is League of Legends. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO2pb4Ji"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2pb4Ji",
                  "id" : "37i9dQZF1DZ06evO2pb4Ji",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO2pb4Ji-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is League of Legends",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDA4NWIyY2RiZjQ0ZmMwY2RkOWRiMzgzOGRlN2E0MTkxZg==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2pb4Ji/tracks",
                    "total" : 51
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO2pb4Ji"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/1drCHIefBw9qd2IgsrF5T7"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/1drCHIefBw9qd2IgsrF5T7",
                  "id" : "1drCHIefBw9qd2IgsrF5T7",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://mosaic.scdn.co/640/ab67616d00001e0214759cc9c2b56299651758f3ab67616d00001e0223ace4cae499c056f055a7deab67616d00001e027942f369dcf5b5d8aca0dc6cab67616d00001e027cff4d64fe43a21c8127371c",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://mosaic.scdn.co/300/ab67616d00001e0214759cc9c2b56299651758f3ab67616d00001e0223ace4cae499c056f055a7deab67616d00001e027942f369dcf5b5d8aca0dc6cab67616d00001e027cff4d64fe43a21c8127371c",
                    "width" : 300
                  }, {
                    "height" : 60,
                    "url" : "https://mosaic.scdn.co/60/ab67616d00001e0214759cc9c2b56299651758f3ab67616d00001e0223ace4cae499c056f055a7deab67616d00001e027942f369dcf5b5d8aca0dc6cab67616d00001e027cff4d64fe43a21c8127371c",
                    "width" : 60
                  } ],
                  "name" : "Mr.Children隠れた名曲",
                  "owner" : {
                    "display_name" : "Chris",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/7ucghdgquf6byqusqkliltwc2"
                    },
                    "href" : "https://api.spotify.com/v1/users/7ucghdgquf6byqusqkliltwc2",
                    "id" : "7ucghdgquf6byqusqkliltwc2",
                    "type" : "user",
                    "uri" : "spotify:user:7ucghdgquf6byqusqkliltwc2"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MzUsNTc1ODVjODZhYjNiMjk5NTBkY2ZkNGQ3ODU1ZTc5Yzc2Mzg0M2Y4NQ==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/1drCHIefBw9qd2IgsrF5T7/tracks",
                    "total" : 30
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:1drCHIefBw9qd2IgsrF5T7"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/6RHTUrRF63xao58xh9FXYJ"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 2273931
                  },
                  "genres" : [ "k-pop", "k-pop girl group" ],
                  "href" : "https://api.spotify.com/v1/artists/6RHTUrRF63xao58xh9FXYJ",
                  "id" : "6RHTUrRF63xao58xh9FXYJ",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebf44daaf3a37f5be9a0721be7",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174f44daaf3a37f5be9a0721be7",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178f44daaf3a37f5be9a0721be7",
                    "width" : 160
                  } ],
                  "name" : "IVE",
                  "popularity" : 78,
                  "type" : "artist",
                  "uri" : "spotify:artist:6RHTUrRF63xao58xh9FXYJ"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/0sYpJ0nCC8AlDrZFeAA7ub"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 698627
                  },
                  "genres" : [ "k-pop" ],
                  "href" : "https://api.spotify.com/v1/artists/0sYpJ0nCC8AlDrZFeAA7ub",
                  "id" : "0sYpJ0nCC8AlDrZFeAA7ub",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebc8166baa6c8c9a5d1dbc41b5",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174c8166baa6c8c9a5d1dbc41b5",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178c8166baa6c8c9a5d1dbc41b5",
                    "width" : 160
                  } ],
                  "name" : "JOY",
                  "popularity" : 51,
                  "type" : "artist",
                  "uri" : "spotify:artist:0sYpJ0nCC8AlDrZFeAA7ub"
                }, {
                  "collaborative" : false,
                  "description" : "今年デビュー30周年を迎えたモンスター・ロックバンド=Mr.Childrenのオールタイム・ベスト。",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXaP96gwPRLee"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXaP96gwPRLee",
                  "id" : "37i9dQZF1DXaP96gwPRLee",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b7ec9c4c033bac8e2d13bf6a",
                    "width" : null
                  } ],
                  "name" : "This Is Mr.Children",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY1NzgxMDgwMCwwMDAwMDAwMGNmZTk0ZjViMjU2YjVjMTI4M2M4YzNiNjY5OTdlNjIw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXaP96gwPRLee/tracks",
                    "total" : 101
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXaP96gwPRLee"
                }, {
                  "collaborative" : false,
                  "description" : "<a href=spotify:playlist:37i9dQZF1EIZfrbfnc9Ur1>Michael Bublé</a>, <a href=spotify:playlist:37i9dQZF1EIWZrySbpUv6m>Margaret Whiting</a>, <a href=spotify:playlist:37i9dQZF1EIUITzJrEPCBA>Nat King Cole</a> and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1EQqA6klNdJvwx"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EQqA6klNdJvwx",
                  "id" : "37i9dQZF1EQqA6klNdJvwx",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seed-mix-image.spotifycdn.com/v6/img/jazz/1GxkXlMwML1oSg5eLPiAz3/en/default",
                    "width" : null
                  } ],
                  "name" : "Jazz Mix",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjQ2MzIwMCwwMDAwMDAwMGJkOWRhYzkyYWRkNWRlM2FmNmIzMzZhZjNmOTg0ZjRh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EQqA6klNdJvwx/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1EQqA6klNdJvwx"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/recently-played?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 107
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/recently-played",
              "id" : "recently-played",
              "images" : [ ],
              "name" : "Recently played",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/made-for-x?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Your weekly mixtape of fresh music. Enjoy new music and deep cuts picked for you. Updates every Monday.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZEVXcU7l4xZKH1Xu"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZEVXcU7l4xZKH1Xu",
                  "id" : "37i9dQZEVXcU7l4xZKH1Xu",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://newjams-images.scdn.co/image/ab676477000033ad/dt/v3/discover-weekly/DXWgm9tgFGm9Iab9Oc6wPPzkzCFjmu9Ze9kRHAwkYMwLrwtoRnNKOUb1EsSsEhhZ9syNmTZ46zywPWCIcz7j6ePUmh0EO6clgdssvfYx0js=/NzI6OTQ6MjFUMjItNjAtMw==",
                    "width" : null
                  } ],
                  "name" : "Discover Weekly",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MCwwMDAwMDAwMGU1MmNlZjIyNzQ2NmU4OTRhNGUzNGNhOTJjOTFiZThh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZEVXcU7l4xZKH1Xu/tracks",
                    "total" : 30
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZEVXcU7l4xZKH1Xu"
                }, {
                  "collaborative" : false,
                  "description" : "Lee Seung Gi, M.O.M, John Park and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E36Y1WP5YoeGC"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E36Y1WP5YoeGC",
                  "id" : "37i9dQZF1E36Y1WP5YoeGC",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab67616d0000b27320d8b3c426601a9b6b2c38b5/1/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 1",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMDZkZGVlNTYzZGFhMDY0MGIwNWViNGExMGMwNTk1Mjk2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E36Y1WP5YoeGC/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E36Y1WP5YoeGC"
                }, {
                  "collaborative" : false,
                  "description" : "Mr.Children, TEE, C&K and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E38moPMess3Gk"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E38moPMess3Gk",
                  "id" : "37i9dQZF1E38moPMess3Gk",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab6761610000e5ebbda4d6cd93e348c967bedd47/2/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 2",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMGEzZTc3YmZiNDRlOTdmODI3MDQ0ZmMyNjI0ODVlZWM3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E38moPMess3Gk/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E38moPMess3Gk"
                }, {
                  "collaborative" : false,
                  "description" : "JVKE, Bruno Major, Fly By Midnight and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E37aUmSiAGVvB"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E37aUmSiAGVvB",
                  "id" : "37i9dQZF1E37aUmSiAGVvB",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab6761610000e5eb8f15a8916c7388aa8c5a896b/3/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 3",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMDE3Y2I1MGExNTMyMTE4MzYyMzY1MDI0YTkwMDQ3YWM0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E37aUmSiAGVvB/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E37aUmSiAGVvB"
                }, {
                  "collaborative" : false,
                  "description" : "Dream Keepers, Skyyy, ame and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E350DyVMohzYB"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E350DyVMohzYB",
                  "id" : "37i9dQZF1E350DyVMohzYB",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab67616d0000b27347bcdc507aeefd928f7808b6/4/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 4",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMGIzZTQ4MTdjZjUxM2UyOWYyZWM1MTkxMjAwZjU5OWYx",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E350DyVMohzYB/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E350DyVMohzYB"
                }, {
                  "collaborative" : false,
                  "description" : "Ikimonogakari, flumpool, Rei Yasuda and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E39PZqpGsGe0u"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E39PZqpGsGe0u",
                  "id" : "37i9dQZF1E39PZqpGsGe0u",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab6761610000e5eb1ff207059a4bb5d90f64b30a/5/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 5",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMGU2MmFhM2E5YTI5ZmU4OTg4N2MxOTExMDg3ZDNjY2Y1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E39PZqpGsGe0u/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E39PZqpGsGe0u"
                }, {
                  "collaborative" : false,
                  "description" : "Tristam, ILLENIUM, Maggie Rogers and more",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E35vE8PEsYnkL"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E35vE8PEsYnkL",
                  "id" : "37i9dQZF1E35vE8PEsYnkL",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://dailymix-images.scdn.co/v2/img/ab6761610000e5eb689bacb06d5215738d72227a/6/en/default",
                    "width" : null
                  } ],
                  "name" : "Daily Mix 6",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0MDUxNCwwMDAwMDAwMDg4MDAxNTUzNjE4ZjJiYTMxNDlhMmE3YWJlN2VhYWYz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E35vE8PEsYnkL/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E35vE8PEsYnkL"
                }, {
                  "collaborative" : false,
                  "description" : "Catch all the latest music from artists you follow, plus new singles picked for you. Updates every Friday.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZEVXbdcnn7uxmjTU"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZEVXbdcnn7uxmjTU",
                  "id" : "37i9dQZEVXbdcnn7uxmjTU",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://newjams-images.scdn.co/image/ab67647800003f8a/dt/v3/release-radar/ab67616d0000b27351c2dca2df814c291f0b7c6a/en-GB",
                    "width" : null
                  } ],
                  "name" : "Release Radar",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MCwwMDAwMDAwMGIxYWI1YzBlNmRmNWFlYTM0YTI2YzQ4Yjk4ODE0OWNj",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZEVXbdcnn7uxmjTU/tracks",
                    "total" : 30
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZEVXbdcnn7uxmjTU"
                } ],
                "limit" : 10,
                "next" : null,
                "offset" : 0,
                "previous" : null,
                "total" : 8
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/made-for-x",
              "id" : "made-for-x",
              "images" : [ ],
              "name" : "Made For Chris",
              "rendering" : "CAROUSEL",
              "tag_line" : "Get better recommendations the more you listen.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/uniquely-yours-shelf?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "We made you a personalized playlist with songs to take you back in time.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1EuSMVlv3344WF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EuSMVlv3344WF",
                  "id" : "37i9dQZF1EuSMVlv3344WF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://daily-mix.scdn.co/covers/time-capsule/time-capsule-blue_DEFAULT-en.jpg",
                    "width" : null
                  } ],
                  "name" : "Your Time Capsule",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjUyODAwMCwwMDAwMDAwMDUzN2QyMjQ1ZGYzZWI4YjhkMGNmMzE1NjI1ZmNiY2I3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EuSMVlv3344WF/tracks",
                    "total" : 0
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1EuSMVlv3344WF"
                }, {
                  "collaborative" : false,
                  "description" : "Songs you love right now",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1EpnUdNkaJFcAv"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EpnUdNkaJFcAv",
                  "id" : "37i9dQZF1EpnUdNkaJFcAv",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://daily-mix.scdn.co/covers/on_repeat/PZN_On_Repeat2_DEFAULT-en.jpg",
                    "width" : null
                  } ],
                  "name" : "On Repeat",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMzNTQwMCwwMDAwMDAwMDQ3MjgzODM2YTAwNzRkOWNmOTdhNzgyOTVmNmQ5MGRk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EpnUdNkaJFcAv/tracks",
                    "total" : 30
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1EpnUdNkaJFcAv"
                }, {
                  "collaborative" : false,
                  "description" : "Your past favorites",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1EpDoJzV4U4PY2"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EpDoJzV4U4PY2",
                  "id" : "37i9dQZF1EpDoJzV4U4PY2",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://daily-mix.scdn.co/covers/backtracks/PZN_Repeat_Rewind2_DEFAULT-en.jpg",
                    "width" : null
                  } ],
                  "name" : "Repeat Rewind",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjA3NzUxNiwwMDAwMDAwMDY1NjE0ZjNhMjJmODE1MWJmMWNlMDFmNmI1YjYzMmJj",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EpDoJzV4U4PY2/tracks",
                    "total" : 27
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1EpDoJzV4U4PY2"
                }, {
                  "collaborative" : false,
                  "description" : "Time for Your Summer Rewind! We’ve made you a new playlist featuring your old summer favorites.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1CAefkC5Sy2Zmg"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1CAefkC5Sy2Zmg",
                  "id" : "37i9dQZF1CAefkC5Sy2Zmg",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://lineup-images.scdn.co/summer-rewind-2020_DEFAULT-en.jpg",
                    "width" : null
                  } ],
                  "name" : "Your Summer Rewind",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjY1NDgxODcsMDAwMDAwMDA2NGQzZDk3OTFkZGM3NzlkMzNkNTdiYWFhMTcyMTcwOA==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1CAefkC5Sy2Zmg/tracks",
                    "total" : 16
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1CAefkC5Sy2Zmg"
                }, {
                  "href" : "https://api.spotify.com/v1/me/tracks",
                  "images" : [ {
                    "url" : "https://misc.scdn.co/liked-songs/liked-songs-300.png"
                  } ],
                  "name" : "Liked Songs",
                  "type" : "link",
                  "uri" : "spotify:collection:tracks"
                }, {
                  "collaborative" : false,
                  "description" : "The songs you loved most this year, all wrapped up.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1EtighSrsGO7Wy"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EtighSrsGO7Wy",
                  "id" : "37i9dQZF1EtighSrsGO7Wy",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://lineup-images.scdn.co/your-top-songs-2019_DEFAULT-en.jpg",
                    "width" : null
                  } ],
                  "name" : "Your Top Songs 2019",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjYyNjk2NzYsMDAwMDAwMDBmYjBlYzA5MGEyNDZhMmUzYzFhZjE1NzcxODAxNzI0YQ==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1EtighSrsGO7Wy/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1EtighSrsGO7Wy"
                } ],
                "limit" : 10,
                "next" : null,
                "offset" : 0,
                "previous" : null,
                "total" : 6
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/uniquely-yours-shelf",
              "id" : "uniquely-yours-shelf",
              "images" : [ {
                "height" : 320,
                "name" : "cover",
                "url" : "https://t.scdn.co/images/1d7768561ef445c8853d69d5718bc161.png",
                "width" : 320
              } ],
              "name" : "Uniquely yours",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/podcast-recs-show-affinity-wrapper?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ ],
                "limit" : 10,
                "next" : null,
                "offset" : 0,
                "previous" : null,
                "total" : 9
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/podcast-recs-show-affinity-wrapper",
              "id" : "podcast-recs-show-affinity-wrapper",
              "images" : [ ],
              "name" : "Your shows",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/programming-local-strategic-playlists?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Voel je goed met deze tijdloze Happy Tunes!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX9u7XXOp0l5L"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9u7XXOp0l5L",
                  "id" : "37i9dQZF1DX9u7XXOp0l5L",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002786e6c6e7e9e87db548ead41",
                    "width" : null
                  } ],
                  "name" : "Happy Tunes",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjU4ODY2MywwMDAwMDAwMDA5Y2JlNzI1MWZkMDAzZGYwOTdjMjMxYWZjMzc0Zjlh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9u7XXOp0l5L/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX9u7XXOp0l5L"
                }, {
                  "collaborative" : false,
                  "description" : "Mooi van eigen bodem. Dus: nationaal in elke taal. Cover: Douwe Bob\n",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1rUSgDt83Z2"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1rUSgDt83Z2",
                  "id" : "37i9dQZF1DX1rUSgDt83Z2",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002c0c3e51c8089b444a452587c",
                    "width" : null
                  } ],
                  "name" : "Made in NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDgyYzgyZjUzODQwZTYzNDFiN2Y0NTY4ZGQyM2RjNDI2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1rUSgDt83Z2/tracks",
                    "total" : 60
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1rUSgDt83Z2"
                }, {
                  "collaborative" : false,
                  "description" : "De 50 populairste hits van Nederland. Cover: Kris Kross Amsterdam, Sofia  Reyes, Tinie Tempah",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSBi5svWQ9Nk"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSBi5svWQ9Nk",
                  "id" : "37i9dQZF1DWSBi5svWQ9Nk",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002e5740593ed7290bd0b3f9379",
                    "width" : null
                  } ],
                  "name" : "Hot Hits NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDc5ZmRjNDE5YmQwMDEyN2M1MDkzNjJiNGRiN2UwYmM4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSBi5svWQ9Nk/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSBi5svWQ9Nk"
                }, {
                  "collaborative" : false,
                  "description" : "Zo klinkt de zomer van 2023! ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXcx1szy2g67M"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcx1szy2g67M",
                  "id" : "37i9dQZF1DXcx1szy2g67M",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000230e97beb0f6cd45ac6486697",
                    "width" : null
                  } ],
                  "name" : "Summer '23",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjQxMzYyNCwwMDAwMDAwMDZkMzgxOGFmOTdhYzVhODY1Y2M2YmFjNTA5YjY2YTNi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcx1szy2g67M/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXcx1szy2g67M"
                }, {
                  "collaborative" : false,
                  "description" : "De grootste Nederlandse hits van vroeger en nu. Cover: Metejoor & Hannah Mae",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdKMCnEhDnDL"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdKMCnEhDnDL",
                  "id" : "37i9dQZF1DXdKMCnEhDnDL",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002101a3c9b9d131a448e7209de",
                    "width" : null
                  } ],
                  "name" : "Beste van NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDEzOTYxMSwwMDAwMDAwMDYyMTFmM2UxMzg0M2VkZjUyNTBhYjc2OTc3ODMyM2Nh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdKMCnEhDnDL/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdKMCnEhDnDL"
                }, {
                  "collaborative" : false,
                  "description" : "Luister naar de fijnste nieuwe songs en hits van dit moment! Cover: Niall Horan ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYSNbqvqvhBQ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYSNbqvqvhBQ",
                  "id" : "37i9dQZF1DWYSNbqvqvhBQ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000021bbb3370877a9e7612dad360",
                    "width" : null
                  } ],
                  "name" : "Stay Tuned!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDRiNzAzM2RjNGI1OTY1ZWI2ZDZmZmYwZDc2OWRmZWYw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYSNbqvqvhBQ/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYSNbqvqvhBQ"
                }, {
                  "collaborative" : false,
                  "description" : "Koffie met gemoedelijke muziek op de achtergrond.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYPwGkJoztcR"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYPwGkJoztcR",
                  "id" : "37i9dQZF1DWYPwGkJoztcR",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000027b57fc0419aa2933af2f9c6b",
                    "width" : null
                  } ],
                  "name" : "'t Koffiehuis",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjQxNzc2MiwwMDAwMDAwMDJmY2Y0Yzg5ZTEyOGFhZWQ0NTYwZWNiNDA2OWU2YThl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYPwGkJoztcR/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYPwGkJoztcR"
                }, {
                  "collaborative" : false,
                  "description" : "De grootste dance hits van juni 2023. Cover: R3HAB & INNA",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWTwCImwcYjDL"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWTwCImwcYjDL",
                  "id" : "37i9dQZF1DWTwCImwcYjDL",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000207d2980b4e004efc3d7d9dc3",
                    "width" : null
                  } ],
                  "name" : "360 Dance",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMGIyZWQ3MzgwZWZkOGEyMjA3Y2ZjYmQ1MTExYWMyODZi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWTwCImwcYjDL/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWTwCImwcYjDL"
                }, {
                  "collaborative" : false,
                  "description" : "Hits om je helemaal mee in het zweet te werken.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX73EtbU4jEcn"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX73EtbU4jEcn",
                  "id" : "37i9dQZF1DX73EtbU4jEcn",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000272ada7a6f873eea879c45c3b",
                    "width" : null
                  } ],
                  "name" : "Top Hits Workout",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjIzOTQzMSwwMDAwMDAwMGRhMWJmZjZmNzUxNmY2NWJlNGU2NGY1ZGE4NjJmNTQ0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX73EtbU4jEcn/tracks",
                    "total" : 90
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX73EtbU4jEcn"
                }, {
                  "collaborative" : false,
                  "description" : "Antoon & Ronnie Flex op de cover van de vernieuwde Je Moerstaal! ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWUX3x84bv557"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUX3x84bv557",
                  "id" : "37i9dQZF1DWUX3x84bv557",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b765703c296ecdb87795fc00",
                    "width" : null
                  } ],
                  "name" : "Je Moerstaal",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDFhNmYwZmVmMTg4M2VjOTU3ODc1YmNiMWViNzYwZDUw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUX3x84bv557/tracks",
                    "total" : 90
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWUX3x84bv557"
                } ],
                "limit" : 10,
                "next" : null,
                "offset" : 0,
                "previous" : null,
                "total" : 10
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/programming-local-strategic-playlists",
              "id" : "programming-local-strategic-playlists",
              "images" : [ ],
              "name" : "Blijf op de hoogte!",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/NMF-NRFY?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/529ZdRwFoSKtQ0LPwKxGiu"
                    },
                    "href" : "https://api.spotify.com/v1/artists/529ZdRwFoSKtQ0LPwKxGiu",
                    "id" : "529ZdRwFoSKtQ0LPwKxGiu",
                    "name" : "Jang Beom June",
                    "type" : "artist",
                    "uri" : "spotify:artist:529ZdRwFoSKtQ0LPwKxGiu"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/5W023cVzDXA2dP1Smgsa1O"
                  },
                  "href" : "https://api.spotify.com/v1/albums/5W023cVzDXA2dP1Smgsa1O",
                  "id" : "5W023cVzDXA2dP1Smgsa1O",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2731dd6bd32114e520618f42c75",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e021dd6bd32114e520618f42c75",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048511dd6bd32114e520618f42c75",
                    "width" : 64
                  } ],
                  "name" : "Can't sleep (Sleep Mix)",
                  "release_date" : "2023-06-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:5W023cVzDXA2dP1Smgsa1O"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/7f5Zgnp2spUuuzKplmRkt7"
                    },
                    "href" : "https://api.spotify.com/v1/artists/7f5Zgnp2spUuuzKplmRkt7",
                    "id" : "7f5Zgnp2spUuuzKplmRkt7",
                    "name" : "Lost Frequencies",
                    "type" : "artist",
                    "uri" : "spotify:artist:7f5Zgnp2spUuuzKplmRkt7"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4Ec0qA1sxuX6vrViAwkxxG"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4Ec0qA1sxuX6vrViAwkxxG",
                  "id" : "4Ec0qA1sxuX6vrViAwkxxG",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273115565fbe2310129fde51a38",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02115565fbe2310129fde51a38",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851115565fbe2310129fde51a38",
                    "width" : 64
                  } ],
                  "name" : "The Feeling",
                  "release_date" : "2023-06-09",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:4Ec0qA1sxuX6vrViAwkxxG"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/4mbvd7ZJ2goftjy1L33LiB"
                    },
                    "href" : "https://api.spotify.com/v1/artists/4mbvd7ZJ2goftjy1L33LiB",
                    "id" : "4mbvd7ZJ2goftjy1L33LiB",
                    "name" : "John Park",
                    "type" : "artist",
                    "uri" : "spotify:artist:4mbvd7ZJ2goftjy1L33LiB"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4X6tSchHruOdYX2GxQtKZb"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4X6tSchHruOdYX2GxQtKZb",
                  "id" : "4X6tSchHruOdYX2GxQtKZb",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2736653faa746e76b668a9dd829",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e026653faa746e76b668a9dd829",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048516653faa746e76b668a9dd829",
                    "width" : 64
                  } ],
                  "name" : "Thought Of You (Sleep Mix)",
                  "release_date" : "2023-06-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:4X6tSchHruOdYX2GxQtKZb"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/1yNI4bivPY5eHMRKaArHxE"
                    },
                    "href" : "https://api.spotify.com/v1/artists/1yNI4bivPY5eHMRKaArHxE",
                    "id" : "1yNI4bivPY5eHMRKaArHxE",
                    "name" : "Lil Droptop Golf Cart",
                    "type" : "artist",
                    "uri" : "spotify:artist:1yNI4bivPY5eHMRKaArHxE"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/5QabrmiuYxbiVw3z13ZxB3"
                  },
                  "href" : "https://api.spotify.com/v1/albums/5QabrmiuYxbiVw3z13ZxB3",
                  "id" : "5QabrmiuYxbiVw3z13ZxB3",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2739a09802c4c4bf109e791e684",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e029a09802c4c4bf109e791e684",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048519a09802c4c4bf109e791e684",
                    "width" : 64
                  } ],
                  "name" : "All These Diamonds",
                  "release_date" : "2023-06-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:5QabrmiuYxbiVw3z13ZxB3"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0LuqEIZz84iXII5583zo2q"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0LuqEIZz84iXII5583zo2q",
                    "id" : "0LuqEIZz84iXII5583zo2q",
                    "name" : "BURT EVE",
                    "type" : "artist",
                    "uri" : "spotify:artist:0LuqEIZz84iXII5583zo2q"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/44xX6kY2X91zluPk8SS29m"
                  },
                  "href" : "https://api.spotify.com/v1/albums/44xX6kY2X91zluPk8SS29m",
                  "id" : "44xX6kY2X91zluPk8SS29m",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273ce7c8a77b01a6595d1c29f6b",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02ce7c8a77b01a6595d1c29f6b",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851ce7c8a77b01a6595d1c29f6b",
                    "width" : 64
                  } ],
                  "name" : "Bless Us",
                  "release_date" : "2023-06-04",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:44xX6kY2X91zluPk8SS29m"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/6lHQqHYVnSr0wPDAa24o6v"
                    },
                    "href" : "https://api.spotify.com/v1/artists/6lHQqHYVnSr0wPDAa24o6v",
                    "id" : "6lHQqHYVnSr0wPDAa24o6v",
                    "name" : "Piibz",
                    "type" : "artist",
                    "uri" : "spotify:artist:6lHQqHYVnSr0wPDAa24o6v"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/0G3lAArfIFhdxjX6daSjhc"
                  },
                  "href" : "https://api.spotify.com/v1/albums/0G3lAArfIFhdxjX6daSjhc",
                  "id" : "0G3lAArfIFhdxjX6daSjhc",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2732922cbea33c10ed57c261500",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e022922cbea33c10ed57c261500",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048512922cbea33c10ed57c261500",
                    "width" : 64
                  } ],
                  "name" : "Sleep Well (Til We Meet Again)",
                  "release_date" : "2023-06-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:0G3lAArfIFhdxjX6daSjhc"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0leKSEjHxsp5fYvQB67kEH"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0leKSEjHxsp5fYvQB67kEH",
                    "id" : "0leKSEjHxsp5fYvQB67kEH",
                    "name" : "FLVR",
                    "type" : "artist",
                    "uri" : "spotify:artist:0leKSEjHxsp5fYvQB67kEH"
                  }, {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/1kwi1Q7X4jPTkHTgayxKhS"
                    },
                    "href" : "https://api.spotify.com/v1/artists/1kwi1Q7X4jPTkHTgayxKhS",
                    "id" : "1kwi1Q7X4jPTkHTgayxKhS",
                    "name" : "Alonestar",
                    "type" : "artist",
                    "uri" : "spotify:artist:1kwi1Q7X4jPTkHTgayxKhS"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/0C5fAUK1vmdIzI9S4lTwqA"
                  },
                  "href" : "https://api.spotify.com/v1/albums/0C5fAUK1vmdIzI9S4lTwqA",
                  "id" : "0C5fAUK1vmdIzI9S4lTwqA",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273436d993fb08358dc7659f6c4",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02436d993fb08358dc7659f6c4",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851436d993fb08358dc7659f6c4",
                    "width" : 64
                  } ],
                  "name" : "Raise Em Up [FLVR Remix] (Chopped & Screwed)",
                  "release_date" : "2023-06-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:0C5fAUK1vmdIzI9S4lTwqA"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/3rxeQlsv0Sc2nyYaZ5W71T"
                    },
                    "href" : "https://api.spotify.com/v1/artists/3rxeQlsv0Sc2nyYaZ5W71T",
                    "id" : "3rxeQlsv0Sc2nyYaZ5W71T",
                    "name" : "Chet Baker",
                    "type" : "artist",
                    "uri" : "spotify:artist:3rxeQlsv0Sc2nyYaZ5W71T"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/6qOPsJFKVjSc78dPvvKfC2"
                  },
                  "href" : "https://api.spotify.com/v1/albums/6qOPsJFKVjSc78dPvvKfC2",
                  "id" : "6qOPsJFKVjSc78dPvvKfC2",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273b8bd55bb04a2e3454f9ad617",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02b8bd55bb04a2e3454f9ad617",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851b8bd55bb04a2e3454f9ad617",
                    "width" : 64
                  } ],
                  "name" : "'Round Midnight (Live In Cologne)",
                  "release_date" : "2023-06-09",
                  "release_date_precision" : "day",
                  "total_tracks" : 5,
                  "type" : "album",
                  "uri" : "spotify:album:6qOPsJFKVjSc78dPvvKfC2"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/NMF-NRFY?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 16
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/NMF-NRFY",
              "id" : "NMF-NRFY",
              "images" : [ ],
              "name" : "New releases for you",
              "rendering" : "CAROUSEL",
              "tag_line" : "Brand new music from artists you love.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Bartist-playlist-for-you%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Known as the Nation's Little Sister in South Korea.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX0y9CwEpdGpz"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0y9CwEpdGpz",
                  "id" : "37i9dQZF1DX0y9CwEpdGpz",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000029c54fc6aef911dcc641972ec",
                    "width" : null
                  } ],
                  "name" : "This Is IU",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzI3NDE2NywwMDAwMDAwMGYwYzYyMzQ3NTliMzk3NTEzZTA5YzJkMjg0ODM1ZGMw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0y9CwEpdGpz/tracks",
                    "total" : 81
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX0y9CwEpdGpz"
                }, {
                  "collaborative" : false,
                  "description" : "可動可靜，能寫能唱，JJ 林俊傑是千禧年後華語樂壇的天王之一。",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWT9HtNMDl9HA"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWT9HtNMDl9HA",
                  "id" : "37i9dQZF1DWT9HtNMDl9HA",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002e9a86e00d77429d6bbc34d5f",
                    "width" : null
                  } ],
                  "name" : "最愛...林俊傑",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjA0NjU2MiwwMDAwMDAwMGVhOTY0NjM1ZTk2N2JlMjA2ZDIyYTcwNzhlYjE0NDlk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWT9HtNMDl9HA/tracks",
                    "total" : 85
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWT9HtNMDl9HA"
                }, {
                  "collaborative" : false,
                  "description" : "力強い歌声で幅広い世代のリスナーを魅了するシンガーソングライター、優里の楽曲を1つのプレイリストに。",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWZBm0zNEjP9K"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZBm0zNEjP9K",
                  "id" : "37i9dQZF1DWZBm0zNEjP9K",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000264ae2d74fbc88c045e380aef",
                    "width" : null
                  } ],
                  "name" : "This Is 優里",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzY4NTA3NCwwMDAwMDAwMGUwMmJmM2I4NzRmNDE4YTU5MGQ1MmU5YTZjMTMyNTQy",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZBm0zNEjP9K/tracks",
                    "total" : 37
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWZBm0zNEjP9K"
                }, {
                  "collaborative" : false,
                  "description" : "The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX5KpP2LN299J"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5KpP2LN299J",
                  "id" : "37i9dQZF1DX5KpP2LN299J",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b0c066c63921e8d43068982c",
                    "width" : null
                  } ],
                  "name" : "This Is Taylor Swift",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTcyNDA2NiwwMDAwMDAwMGE4Yjg4YTY0MTVkZTc0YmJkODc0OWFmMDA1MDVhMjY5",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5KpP2LN299J/tracks",
                    "total" : 153
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX5KpP2LN299J"
                }, {
                  "collaborative" : false,
                  "description" : "This is Lee Seung Gi. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO0yofXq"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO0yofXq",
                  "id" : "37i9dQZF1DZ06evO0yofXq",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO0yofXq-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is Lee Seung Gi",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDAwYmUyZGUyOTUzMGJlMjI4YzA4NWQ5ZTU1ZWY2Nzk1YQ==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO0yofXq/tracks",
                    "total" : 48
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO0yofXq"
                }, {
                  "collaborative" : false,
                  "description" : "This is Michael Bublé. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO0WqnZe"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO0WqnZe",
                  "id" : "37i9dQZF1DZ06evO0WqnZe",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO0WqnZe-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is Michael Bublé",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDAwYTIyMTcyOTA3MTQzZjcwNDliYjk2M2I1OTQzNjUxZA==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO0WqnZe/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO0WqnZe"
                }, {
                  "collaborative" : false,
                  "description" : "This is League of Legends. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO2pb4Ji"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2pb4Ji",
                  "id" : "37i9dQZF1DZ06evO2pb4Ji",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO2pb4Ji-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is League of Legends",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDA4NWIyY2RiZjQ0ZmMwY2RkOWRiMzgzOGRlN2E0MTkxZg==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2pb4Ji/tracks",
                    "total" : 51
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO2pb4Ji"
                }, {
                  "collaborative" : false,
                  "description" : "This is Jang Beom June. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO2XlOyS"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2XlOyS",
                  "id" : "37i9dQZF1DZ06evO2XlOyS",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO2XlOyS-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is Jang Beom June",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDAzYjg5MTgyOTkwZjRhMGI5MjEzZWM1YjkxN2JmZDM2ZA==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO2XlOyS/tracks",
                    "total" : 29
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO2XlOyS"
                }, {
                  "collaborative" : false,
                  "description" : "This is Chris Hart. The essential tracks, all in one playlist.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DZ06evO4yZrNp"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO4yZrNp",
                  "id" : "37i9dQZF1DZ06evO4yZrNp",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://thisis-images.scdn.co/37i9dQZF1DZ06evO4yZrNp-default.jpg",
                    "width" : null
                  } ],
                  "name" : "This Is Chris Hart",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MjgxMDgwOTgsMDAwMDAwMDA1N2JhNTY5MTM1NDc4MDgxODM2NzBiMzM5ZWVjYmRiNw==",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DZ06evO4yZrNp/tracks",
                    "total" : 46
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DZ06evO4yZrNp"
                }, {
                  "collaborative" : false,
                  "description" : "back numberのオール・タイム・ベスト。",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8areMEHPwto"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8areMEHPwto",
                  "id" : "37i9dQZF1DX8areMEHPwto",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002712c7cc8c403842ff0b3b822",
                    "width" : null
                  } ],
                  "name" : "This Is back number",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3Mzg4MTIwMCwwMDAwMDAwMDQzNThjNGJjNzljYjFjZWY4YmMyYjQ4N2IzNTc5YzA4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8areMEHPwto/tracks",
                    "total" : 62
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8areMEHPwto"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Bartist-playlist-for-you%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 20
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Bartist-playlist-for-you%5D",
              "id" : "home-personalized[artist-playlist-for-you]",
              "images" : [ ],
              "name" : "Best of artists",
              "rendering" : "CAROUSEL",
              "tag_line" : "Bringing together the top songs from an artist.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Bfavorite-artists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/7jFUYMpMUBDL4JQtMZ5ilc"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 204492
                  },
                  "genres" : [ "korean pop" ],
                  "href" : "https://api.spotify.com/v1/artists/7jFUYMpMUBDL4JQtMZ5ilc",
                  "id" : "7jFUYMpMUBDL4JQtMZ5ilc",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebb3c06b25c1c87dfcec00877d",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174b3c06b25c1c87dfcec00877d",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178b3c06b25c1c87dfcec00877d",
                    "width" : 160
                  } ],
                  "name" : "Sung Si Kyung",
                  "popularity" : 50,
                  "type" : "artist",
                  "uri" : "spotify:artist:7jFUYMpMUBDL4JQtMZ5ilc"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/7bWYN0sHvyH7yv1uefX07U"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 82635
                  },
                  "genres" : [ "korean pop" ],
                  "href" : "https://api.spotify.com/v1/artists/7bWYN0sHvyH7yv1uefX07U",
                  "id" : "7bWYN0sHvyH7yv1uefX07U",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb30fb47dd22deb36bf0175501",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab6761610000517430fb47dd22deb36bf0175501",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f17830fb47dd22deb36bf0175501",
                    "width" : 160
                  } ],
                  "name" : "Jukjae",
                  "popularity" : 46,
                  "type" : "artist",
                  "uri" : "spotify:artist:7bWYN0sHvyH7yv1uefX07U"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/0GsGBWIkeFJxFllGUemX5i"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 1662
                  },
                  "genres" : [ ],
                  "href" : "https://api.spotify.com/v1/artists/0GsGBWIkeFJxFllGUemX5i",
                  "id" : "0GsGBWIkeFJxFllGUemX5i",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27364e26ee1b504427d32c81d9b",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0264e26ee1b504427d32c81d9b",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485164e26ee1b504427d32c81d9b",
                    "width" : 64
                  } ],
                  "name" : "Jinyoung",
                  "popularity" : 19,
                  "type" : "artist",
                  "uri" : "spotify:artist:0GsGBWIkeFJxFllGUemX5i"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/7k73EtZwoPs516ZxE72KsO"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 4966162
                  },
                  "genres" : [ "j-pop", "j-rock", "japanese emo" ],
                  "href" : "https://api.spotify.com/v1/artists/7k73EtZwoPs516ZxE72KsO",
                  "id" : "7k73EtZwoPs516ZxE72KsO",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb4900a06db4e96dd1444300d4",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab676161000051744900a06db4e96dd1444300d4",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f1784900a06db4e96dd1444300d4",
                    "width" : 160
                  } ],
                  "name" : "ONE OK ROCK",
                  "popularity" : 70,
                  "type" : "artist",
                  "uri" : "spotify:artist:7k73EtZwoPs516ZxE72KsO"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/3iRqbMhzyOyoCkmmMRxLWR"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 153749
                  },
                  "genres" : [ "mainland chinese pop", "mandopop" ],
                  "href" : "https://api.spotify.com/v1/artists/3iRqbMhzyOyoCkmmMRxLWR",
                  "id" : "3iRqbMhzyOyoCkmmMRxLWR",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb742c86f458294c8085cc8f61",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174742c86f458294c8085cc8f61",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178742c86f458294c8085cc8f61",
                    "width" : 160
                  } ],
                  "name" : "Hu Xia",
                  "popularity" : 47,
                  "type" : "artist",
                  "uri" : "spotify:artist:3iRqbMhzyOyoCkmmMRxLWR"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/0ixzjrK1wkN2zWBXt3VW3W"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 1864127
                  },
                  "genres" : [ "j-pop", "japanese singer-songwriter", "japanese teen pop" ],
                  "href" : "https://api.spotify.com/v1/artists/0ixzjrK1wkN2zWBXt3VW3W",
                  "id" : "0ixzjrK1wkN2zWBXt3VW3W",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb2a98f9ecf7217c8f910f9f83",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab676161000051742a98f9ecf7217c8f910f9f83",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f1782a98f9ecf7217c8f910f9f83",
                    "width" : 160
                  } ],
                  "name" : "Yuuri",
                  "popularity" : 70,
                  "type" : "artist",
                  "uri" : "spotify:artist:0ixzjrK1wkN2zWBXt3VW3W"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/3HqSLMAZ3g3d5poNaI7GOU"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 8006856
                  },
                  "genres" : [ "k-pop", "pop" ],
                  "href" : "https://api.spotify.com/v1/artists/3HqSLMAZ3g3d5poNaI7GOU",
                  "id" : "3HqSLMAZ3g3d5poNaI7GOU",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb006ff3c0136a71bfb9928d34",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174006ff3c0136a71bfb9928d34",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178006ff3c0136a71bfb9928d34",
                    "width" : 160
                  } ],
                  "name" : "IU",
                  "popularity" : 73,
                  "type" : "artist",
                  "uri" : "spotify:artist:3HqSLMAZ3g3d5poNaI7GOU"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/6rs1KAoQnFalSqSU4LTh8g"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 3849136
                  },
                  "genres" : [ "j-pop", "j-rock" ],
                  "href" : "https://api.spotify.com/v1/artists/6rs1KAoQnFalSqSU4LTh8g",
                  "id" : "6rs1KAoQnFalSqSU4LTh8g",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebe96115c48f043bbd7deaa298",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174e96115c48f043bbd7deaa298",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178e96115c48f043bbd7deaa298",
                    "width" : 160
                  } ],
                  "name" : "back number",
                  "popularity" : 71,
                  "type" : "artist",
                  "uri" : "spotify:artist:6rs1KAoQnFalSqSU4LTh8g"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/12AUp9oqeJDhNfO6IhQiNi"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 168550
                  },
                  "genres" : [ "korean pop" ],
                  "href" : "https://api.spotify.com/v1/artists/12AUp9oqeJDhNfO6IhQiNi",
                  "id" : "12AUp9oqeJDhNfO6IhQiNi",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27320d8b3c426601a9b6b2c38b5",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0220d8b3c426601a9b6b2c38b5",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485120d8b3c426601a9b6b2c38b5",
                    "width" : 64
                  } ],
                  "name" : "Lee Seung Gi",
                  "popularity" : 40,
                  "type" : "artist",
                  "uri" : "spotify:artist:12AUp9oqeJDhNfO6IhQiNi"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/706WzkJEacBrtkHKRpBU2q"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 988313
                  },
                  "genres" : [ "j-acoustic", "j-pop", "j-rock", "japanese singer-songwriter" ],
                  "href" : "https://api.spotify.com/v1/artists/706WzkJEacBrtkHKRpBU2q",
                  "id" : "706WzkJEacBrtkHKRpBU2q",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb921f500e05dcf53fc9b6d807",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174921f500e05dcf53fc9b6d807",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178921f500e05dcf53fc9b6d807",
                    "width" : 160
                  } ],
                  "name" : "Motohiro Hata",
                  "popularity" : 55,
                  "type" : "artist",
                  "uri" : "spotify:artist:706WzkJEacBrtkHKRpBU2q"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Bfavorite-artists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 20
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Bfavorite-artists%5D",
              "id" : "home-personalized[favorite-artists]",
              "images" : [ ],
              "name" : "Your favorite artists",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-albums%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/7y3HnWCFEvWj4KM9GFSkiX"
                    },
                    "href" : "https://api.spotify.com/v1/artists/7y3HnWCFEvWj4KM9GFSkiX",
                    "id" : "7y3HnWCFEvWj4KM9GFSkiX",
                    "name" : "WeiBird",
                    "type" : "artist",
                    "uri" : "spotify:artist:7y3HnWCFEvWj4KM9GFSkiX"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/6CGKNcn63JbPWljHtQi1L0"
                  },
                  "href" : "https://api.spotify.com/v1/albums/6CGKNcn63JbPWljHtQi1L0",
                  "id" : "6CGKNcn63JbPWljHtQi1L0",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273d8bd453784ae431700a851b0",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02d8bd453784ae431700a851b0",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851d8bd453784ae431700a851b0",
                    "width" : 64
                  } ],
                  "name" : "如果可以 (電影\"月老\"主題曲)",
                  "release_date" : "2021-11-05",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:6CGKNcn63JbPWljHtQi1L0"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0K87f3owemzI8NUCoEIXOB"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0K87f3owemzI8NUCoEIXOB",
                    "id" : "0K87f3owemzI8NUCoEIXOB",
                    "name" : "vaultboy",
                    "type" : "artist",
                    "uri" : "spotify:artist:0K87f3owemzI8NUCoEIXOB"
                  }, {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/2FLqlgckDKdmpBrvLAT5BM"
                    },
                    "href" : "https://api.spotify.com/v1/artists/2FLqlgckDKdmpBrvLAT5BM",
                    "id" : "2FLqlgckDKdmpBrvLAT5BM",
                    "name" : "Eric Nam",
                    "type" : "artist",
                    "uri" : "spotify:artist:2FLqlgckDKdmpBrvLAT5BM"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/3mx2H1xWJmafdZAH06TEHI"
                  },
                  "href" : "https://api.spotify.com/v1/albums/3mx2H1xWJmafdZAH06TEHI",
                  "id" : "3mx2H1xWJmafdZAH06TEHI",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273c66bfff6abcfa8b983e62e31",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02c66bfff6abcfa8b983e62e31",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851c66bfff6abcfa8b983e62e31",
                    "width" : 64
                  } ],
                  "name" : "everything sucks (feat. Eric Nam)",
                  "release_date" : "2021-08-13",
                  "release_date_precision" : "day",
                  "total_tracks" : 2,
                  "type" : "album",
                  "uri" : "spotify:album:3mx2H1xWJmafdZAH06TEHI"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0ixzjrK1wkN2zWBXt3VW3W"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0ixzjrK1wkN2zWBXt3VW3W",
                    "id" : "0ixzjrK1wkN2zWBXt3VW3W",
                    "name" : "Yuuri",
                    "type" : "artist",
                    "uri" : "spotify:artist:0ixzjrK1wkN2zWBXt3VW3W"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/6yQZ7vFWS1fWkDtI2QZaC2"
                  },
                  "href" : "https://api.spotify.com/v1/albums/6yQZ7vFWS1fWkDtI2QZaC2",
                  "id" : "6yQZ7vFWS1fWkDtI2QZaC2",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27392c4d53480960aad48d005b8",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0292c4d53480960aad48d005b8",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485192c4d53480960aad48d005b8",
                    "width" : 64
                  } ],
                  "name" : "Dried Flowers(English ver.)",
                  "release_date" : "2022-02-01",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:6yQZ7vFWS1fWkDtI2QZaC2"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/6VuMaDnrHyPL1p4EHjYLi7"
                    },
                    "href" : "https://api.spotify.com/v1/artists/6VuMaDnrHyPL1p4EHjYLi7",
                    "id" : "6VuMaDnrHyPL1p4EHjYLi7",
                    "name" : "Charlie Puth",
                    "type" : "artist",
                    "uri" : "spotify:artist:6VuMaDnrHyPL1p4EHjYLi7"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/2LTqBgZUH4EkDcj8hdkNjK"
                  },
                  "href" : "https://api.spotify.com/v1/albums/2LTqBgZUH4EkDcj8hdkNjK",
                  "id" : "2LTqBgZUH4EkDcj8hdkNjK",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273a3b39c1651a617bb09800fd8",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02a3b39c1651a617bb09800fd8",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851a3b39c1651a617bb09800fd8",
                    "width" : 64
                  } ],
                  "name" : "CHARLIE",
                  "release_date" : "2022-10-07",
                  "release_date_precision" : "day",
                  "total_tracks" : 12,
                  "type" : "album",
                  "uri" : "spotify:album:2LTqBgZUH4EkDcj8hdkNjK"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0kX41bvrBQtgqSEXbmTzMN"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0kX41bvrBQtgqSEXbmTzMN",
                    "id" : "0kX41bvrBQtgqSEXbmTzMN",
                    "name" : "eaJ",
                    "type" : "artist",
                    "uri" : "spotify:artist:0kX41bvrBQtgqSEXbmTzMN"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/0Ue5YoruxCJPmb8M14BeEP"
                  },
                  "href" : "https://api.spotify.com/v1/albums/0Ue5YoruxCJPmb8M14BeEP",
                  "id" : "0Ue5YoruxCJPmb8M14BeEP",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273537f0cc27d06681a55dc38e8",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02537f0cc27d06681a55dc38e8",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851537f0cc27d06681a55dc38e8",
                    "width" : 64
                  } ],
                  "name" : "Car Crash",
                  "release_date" : "2022-04-08",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:0Ue5YoruxCJPmb8M14BeEP"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/5tRk0bqMQubKAVowp35XtC"
                    },
                    "href" : "https://api.spotify.com/v1/artists/5tRk0bqMQubKAVowp35XtC",
                    "id" : "5tRk0bqMQubKAVowp35XtC",
                    "name" : "MC 張天賦",
                    "type" : "artist",
                    "uri" : "spotify:artist:5tRk0bqMQubKAVowp35XtC"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/34BPcfbDQkYaJLrCgrEwYx"
                  },
                  "href" : "https://api.spotify.com/v1/albums/34BPcfbDQkYaJLrCgrEwYx",
                  "id" : "34BPcfbDQkYaJLrCgrEwYx",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273efde350949d2ef3d410583cc",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02efde350949d2ef3d410583cc",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851efde350949d2ef3d410583cc",
                    "width" : 64
                  } ],
                  "name" : "This is MC",
                  "release_date" : "2023-01-20",
                  "release_date_precision" : "day",
                  "total_tracks" : 9,
                  "type" : "album",
                  "uri" : "spotify:album:34BPcfbDQkYaJLrCgrEwYx"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/4jGPdu95icCKVF31CcFKbS"
                    },
                    "href" : "https://api.spotify.com/v1/artists/4jGPdu95icCKVF31CcFKbS",
                    "id" : "4jGPdu95icCKVF31CcFKbS",
                    "name" : "Gentle Bones",
                    "type" : "artist",
                    "uri" : "spotify:artist:4jGPdu95icCKVF31CcFKbS"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4BEjRqKkO7zvPO6GXCDcIM"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4BEjRqKkO7zvPO6GXCDcIM",
                  "id" : "4BEjRqKkO7zvPO6GXCDcIM",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273b73548fd843e8712c683bc15",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02b73548fd843e8712c683bc15",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851b73548fd843e8712c683bc15",
                    "width" : 64
                  } ],
                  "name" : "Gentle Bones",
                  "release_date" : "2022-01-05",
                  "release_date_precision" : "day",
                  "total_tracks" : 9,
                  "type" : "album",
                  "uri" : "spotify:album:4BEjRqKkO7zvPO6GXCDcIM"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0HJsX1aTdgG1VDIRDiseSJ"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0HJsX1aTdgG1VDIRDiseSJ",
                    "id" : "0HJsX1aTdgG1VDIRDiseSJ",
                    "name" : "Sarah Barrios",
                    "type" : "artist",
                    "uri" : "spotify:artist:0HJsX1aTdgG1VDIRDiseSJ"
                  }, {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/2FLqlgckDKdmpBrvLAT5BM"
                    },
                    "href" : "https://api.spotify.com/v1/artists/2FLqlgckDKdmpBrvLAT5BM",
                    "id" : "2FLqlgckDKdmpBrvLAT5BM",
                    "name" : "Eric Nam",
                    "type" : "artist",
                    "uri" : "spotify:artist:2FLqlgckDKdmpBrvLAT5BM"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/3CugoYr2fheAKsUkc4wsVo"
                  },
                  "href" : "https://api.spotify.com/v1/albums/3CugoYr2fheAKsUkc4wsVo",
                  "id" : "3CugoYr2fheAKsUkc4wsVo",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2738c6b9a8716162fb5f3fa4164",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e028c6b9a8716162fb5f3fa4164",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048518c6b9a8716162fb5f3fa4164",
                    "width" : 64
                  } ],
                  "name" : "Have We Met Before (with Eric Nam)",
                  "release_date" : "2021-04-30",
                  "release_date_precision" : "day",
                  "total_tracks" : 1,
                  "type" : "album",
                  "uri" : "spotify:album:3CugoYr2fheAKsUkc4wsVo"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0lgENJQUkqkDbpsTYEayOr"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0lgENJQUkqkDbpsTYEayOr",
                    "id" : "0lgENJQUkqkDbpsTYEayOr",
                    "name" : "JUNNY",
                    "type" : "artist",
                    "uri" : "spotify:artist:0lgENJQUkqkDbpsTYEayOr"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/2yqOMxorfrYXbnVkjcaq5y"
                  },
                  "href" : "https://api.spotify.com/v1/albums/2yqOMxorfrYXbnVkjcaq5y",
                  "id" : "2yqOMxorfrYXbnVkjcaq5y",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2731f4f14c30d1da4d9a7f40ff7",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e021f4f14c30d1da4d9a7f40ff7",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048511f4f14c30d1da4d9a7f40ff7",
                    "width" : 64
                  } ],
                  "name" : "MOVIE",
                  "release_date" : "2020-09-20",
                  "release_date_precision" : "day",
                  "total_tracks" : 2,
                  "type" : "album",
                  "uri" : "spotify:album:2yqOMxorfrYXbnVkjcaq5y"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/72nLe76yBFSlP6VBzME358"
                    },
                    "href" : "https://api.spotify.com/v1/artists/72nLe76yBFSlP6VBzME358",
                    "id" : "72nLe76yBFSlP6VBzME358",
                    "name" : "SHAUN",
                    "type" : "artist",
                    "uri" : "spotify:artist:72nLe76yBFSlP6VBzME358"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/1hHfFi6UbGjES8cTAmlNYs"
                  },
                  "href" : "https://api.spotify.com/v1/albums/1hHfFi6UbGjES8cTAmlNYs",
                  "id" : "1hHfFi6UbGjES8cTAmlNYs",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273ad364eab28862df117b7f40b",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02ad364eab28862df117b7f40b",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851ad364eab28862df117b7f40b",
                    "width" : 64
                  } ],
                  "name" : "#0055b7",
                  "release_date" : "2021-05-09",
                  "release_date_precision" : "day",
                  "total_tracks" : 2,
                  "type" : "album",
                  "uri" : "spotify:album:1hHfFi6UbGjES8cTAmlNYs"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-albums%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 15
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-albums%5D",
              "id" : "home-personalized[inspired-by-recent-albums]",
              "images" : [ ],
              "name" : "Recommended for today",
              "rendering" : "CAROUSEL",
              "tag_line" : "Inspired by your recent activity.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-artists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/0kX41bvrBQtgqSEXbmTzMN"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 137661
                  },
                  "genres" : [ ],
                  "href" : "https://api.spotify.com/v1/artists/0kX41bvrBQtgqSEXbmTzMN",
                  "id" : "0kX41bvrBQtgqSEXbmTzMN",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebffb8f54390e4a07986d66581",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174ffb8f54390e4a07986d66581",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178ffb8f54390e4a07986d66581",
                    "width" : 160
                  } ],
                  "name" : "eaJ",
                  "popularity" : 55,
                  "type" : "artist",
                  "uri" : "spotify:artist:0kX41bvrBQtgqSEXbmTzMN"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/6maAVJxVTGW1xA3LokpQm8"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 287119
                  },
                  "genres" : [ "otacore" ],
                  "href" : "https://api.spotify.com/v1/artists/6maAVJxVTGW1xA3LokpQm8",
                  "id" : "6maAVJxVTGW1xA3LokpQm8",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb181a909eb13bbe013eeb7708",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174181a909eb13bbe013eeb7708",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178181a909eb13bbe013eeb7708",
                    "width" : 160
                  } ],
                  "name" : "LilyPichu",
                  "popularity" : 50,
                  "type" : "artist",
                  "uri" : "spotify:artist:6maAVJxVTGW1xA3LokpQm8"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/3tvtGR8HzMHDbkLeZrFiBI"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 170273
                  },
                  "genres" : [ "cantopop" ],
                  "href" : "https://api.spotify.com/v1/artists/3tvtGR8HzMHDbkLeZrFiBI",
                  "id" : "3tvtGR8HzMHDbkLeZrFiBI",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb084d3b40efaaa02e52a881ff",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174084d3b40efaaa02e52a881ff",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178084d3b40efaaa02e52a881ff",
                    "width" : 160
                  } ],
                  "name" : "Terence Lam",
                  "popularity" : 53,
                  "type" : "artist",
                  "uri" : "spotify:artist:3tvtGR8HzMHDbkLeZrFiBI"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/3faWjSGISqopPb4VqxHWaZ"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 18141
                  },
                  "genres" : [ "game mood" ],
                  "href" : "https://api.spotify.com/v1/artists/3faWjSGISqopPb4VqxHWaZ",
                  "id" : "3faWjSGISqopPb4VqxHWaZ",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebabccdc38177edfbd62b220fc",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174abccdc38177edfbd62b220fc",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178abccdc38177edfbd62b220fc",
                    "width" : 160
                  } ],
                  "name" : "Natsumiii",
                  "popularity" : 32,
                  "type" : "artist",
                  "uri" : "spotify:artist:3faWjSGISqopPb4VqxHWaZ"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/4HqIcgpeGKabzBYczmfFgZ"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 2603
                  },
                  "genres" : [ "singaporean pop" ],
                  "href" : "https://api.spotify.com/v1/artists/4HqIcgpeGKabzBYczmfFgZ",
                  "id" : "4HqIcgpeGKabzBYczmfFgZ",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eba12ee039481993b98a54d55c",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174a12ee039481993b98a54d55c",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178a12ee039481993b98a54d55c",
                    "width" : 160
                  } ],
                  "name" : "Pseudo",
                  "popularity" : 27,
                  "type" : "artist",
                  "uri" : "spotify:artist:4HqIcgpeGKabzBYczmfFgZ"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/56zJ6PZ3mNPBiBqglW2KxL"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 48047
                  },
                  "genres" : [ "norwegian pop" ],
                  "href" : "https://api.spotify.com/v1/artists/56zJ6PZ3mNPBiBqglW2KxL",
                  "id" : "56zJ6PZ3mNPBiBqglW2KxL",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb08a414cb31e7f07d73f35a8f",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab6761610000517408a414cb31e7f07d73f35a8f",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f17808a414cb31e7f07d73f35a8f",
                    "width" : 160
                  } ],
                  "name" : "Peder Elias",
                  "popularity" : 54,
                  "type" : "artist",
                  "uri" : "spotify:artist:56zJ6PZ3mNPBiBqglW2KxL"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/3wYcmejLVtOoHIq9szUugh"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 42762
                  },
                  "genres" : [ "cantopop", "hk-pop" ],
                  "href" : "https://api.spotify.com/v1/artists/3wYcmejLVtOoHIq9szUugh",
                  "id" : "3wYcmejLVtOoHIq9szUugh",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebdb63d68c8791023da9df0afd",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174db63d68c8791023da9df0afd",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178db63d68c8791023da9df0afd",
                    "width" : 160
                  } ],
                  "name" : "Kaho Hung",
                  "popularity" : 50,
                  "type" : "artist",
                  "uri" : "spotify:artist:3wYcmejLVtOoHIq9szUugh"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/0yknwn0XnsbFLagS80AA0n"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 19487
                  },
                  "genres" : [ "hk-pop" ],
                  "href" : "https://api.spotify.com/v1/artists/0yknwn0XnsbFLagS80AA0n",
                  "id" : "0yknwn0XnsbFLagS80AA0n",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb435443bd681868313da4c709",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174435443bd681868313da4c709",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178435443bd681868313da4c709",
                    "width" : 160
                  } ],
                  "name" : "Byejack",
                  "popularity" : 44,
                  "type" : "artist",
                  "uri" : "spotify:artist:0yknwn0XnsbFLagS80AA0n"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/2BvHYGyThg2Lrm67TbzBp9"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 2222
                  },
                  "genres" : [ ],
                  "href" : "https://api.spotify.com/v1/artists/2BvHYGyThg2Lrm67TbzBp9",
                  "id" : "2BvHYGyThg2Lrm67TbzBp9",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5eb47ffb2261a1fd40ed66544d7",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab6761610000517447ffb2261a1fd40ed66544d7",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f17847ffb2261a1fd40ed66544d7",
                    "width" : 160
                  } ],
                  "name" : "good gasoline",
                  "popularity" : 24,
                  "type" : "artist",
                  "uri" : "spotify:artist:2BvHYGyThg2Lrm67TbzBp9"
                }, {
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/artist/74DSMvAfXpnN3c1KCfvFwQ"
                  },
                  "followers" : {
                    "href" : null,
                    "total" : 30828
                  },
                  "genres" : [ "indonesian singer-songwriter" ],
                  "href" : "https://api.spotify.com/v1/artists/74DSMvAfXpnN3c1KCfvFwQ",
                  "id" : "74DSMvAfXpnN3c1KCfvFwQ",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab6761610000e5ebe0f90c46720c3e0e29a5cf5a",
                    "width" : 640
                  }, {
                    "height" : 320,
                    "url" : "https://i.scdn.co/image/ab67616100005174e0f90c46720c3e0e29a5cf5a",
                    "width" : 320
                  }, {
                    "height" : 160,
                    "url" : "https://i.scdn.co/image/ab6761610000f178e0f90c46720c3e0e29a5cf5a",
                    "width" : 160
                  } ],
                  "name" : "Chris Andrian Yang",
                  "popularity" : 43,
                  "type" : "artist",
                  "uri" : "spotify:artist:74DSMvAfXpnN3c1KCfvFwQ"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-artists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 15
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-artists%5D",
              "id" : "home-personalized[inspired-by-recent-artists]",
              "images" : [ ],
              "name" : "Suggested artists",
              "rendering" : "CAROUSEL",
              "tag_line" : "Inspired by your recent activity.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-playlists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Listen to the soundtrack of the SBS Mon-Tue drama CHEER UP and the songs recommended by the cast! (SBS 월, 화 드라마 치얼업의 사운드 트랙과 배우진이 추천하는 노래를 들어보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX4Z5FphlroPF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4Z5FphlroPF",
                  "id" : "37i9dQZF1DX4Z5FphlroPF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000021aa954322c0dc7e0f00bb2a7",
                    "width" : null
                  } ],
                  "name" : "CHEER UP (치얼업)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3MDI5NzY4MywwMDAwMDAwMGE5MDk5MmFiOGIwMWZmNDAxY2VjN2E0MGI1MDk2Y2Q0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4Z5FphlroPF/tracks",
                    "total" : 33
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX4Z5FphlroPF"
                }, {
                  "collaborative" : false,
                  "description" : "Listen to the soundtracks of Disney+'s original drama Call It Love. (디즈니+ 오리지널 드라마 사랑이라 말해요의 사운드 트랙들을 즐겨보세요!) You can also listen to 15 songs personally selected by actor Kim Young-kwang. (김영광 배우가 직접 선정한 15곡도 들어보실 수 있습니다.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYJer66eoqCP"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYJer66eoqCP",
                  "id" : "37i9dQZF1DWYJer66eoqCP",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002f1728adc8dcc7ead01c0a756",
                    "width" : null
                  } ],
                  "name" : "사랑이라 말해요(Call It Love)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3OTkxMzk3MywwMDAwMDAwMGRiZTU2ZTY1MzM1NTkyODgwZjliYTk2NzYyODJmNTY4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYJer66eoqCP/tracks",
                    "total" : 28
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYJer66eoqCP"
                }, {
                  "collaborative" : false,
                  "description" : "Chillout to the coolest Korean acoustic tunes. (Cover: 10cm) (감미롭고 부드러운 한국 어쿠스틱 음악과 함께하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1wdZM1FEz79"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79",
                  "id" : "37i9dQZF1DX1wdZM1FEz79",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002c33d807b43a03e0fceac8559",
                    "width" : null
                  } ],
                  "name" : "K-Pop Acoustic",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDEyNjI2NSwwMDAwMDAwMDQ0ZDE3YjQ5Njc3YzlkZGI0MjU1MjMyMTkyMmU3Y2Q4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79/tracks",
                    "total" : 52
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1wdZM1FEz79"
                }, {
                  "collaborative" : false,
                  "description" : "Time to press play on these jaem jams from 2010 onwards! Cover: MONSTA X",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdR77H5Z8MIM"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM",
                  "id" : "37i9dQZF1DXdR77H5Z8MIM",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002dd04c8560daaa69bddfd25e6",
                    "width" : null
                  } ],
                  "name" : "Nolja!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjQ3NzI1OCwwMDAwMDAwMDBjYzY4YmVjY2Y0OWFkMWNkMDNjNThhZTdhODk3Njkw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM/tracks",
                    "total" : 70
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdR77H5Z8MIM"
                }, {
                  "collaborative" : false,
                  "description" : "Ready for some sweet harmony or explosive combination of your favourite couple or troublemakers. (Cover: SOLE, Sung Si Kyung)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWZYjbSZYSpu6"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZYjbSZYSpu6",
                  "id" : "37i9dQZF1DWZYjbSZYSpu6",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000023e12bbaa9eac9426deaae2b1",
                    "width" : null
                  } ],
                  "name" : "K-Pop Duets (러블리 듀엣)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzAwNzg4NiwwMDAwMDAwMDNjNDI0MTQ4N2QxNzZkODAxNDE1MGVlMjgxMDQ3YTY3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZYjbSZYSpu6/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWZYjbSZYSpu6"
                }, {
                  "collaborative" : false,
                  "description" : "Chill Korean tunes that's perfect with your latte or americano. (카페와 어울리는 편안한 음악들을 감상하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX5g856aiKiDS"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS",
                  "id" : "37i9dQZF1DX5g856aiKiDS",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000262a9874085e91a05440a1cee",
                    "width" : null
                  } ],
                  "name" : "Dalkom Cafe",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTU0ODE0NCwwMDAwMDAwMDgyZWFlZDJiMjU3ZGFhYTU3ZWE5YmVkNDRlODg5ODQ3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX5g856aiKiDS"
                }, {
                  "collaborative" : false,
                  "description" : "Watch out for all the collaborations and cross-overs in the K-Pop world! (Cover: KANG DANIEL)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX4IDaXtVjL83"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4IDaXtVjL83",
                  "id" : "37i9dQZF1DX4IDaXtVjL83",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000023186f9f9fc5ecdc7a639ce3f",
                    "width" : null
                  } ],
                  "name" : "K-Pop X-Overs",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjIwOTk5NywwMDAwMDAwMGRmYThmNjdmNGJlNjA1YmM1MmY0MTJhMGRkMGU3NmJl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4IDaXtVjL83/tracks",
                    "total" : 66
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX4IDaXtVjL83"
                }, {
                  "collaborative" : false,
                  "description" : "Relive your favourite K-Drama moments with these classic soundtracks.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWUXxc8Mc6MmJ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUXxc8Mc6MmJ",
                  "id" : "37i9dQZF1DWUXxc8Mc6MmJ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000248af925fcc4cf111f8b1fc76",
                    "width" : null
                  } ],
                  "name" : "Best of Korean Soundtracks",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3ODY4NzQzNiwwMDAwMDAwMDAzMDllYTgzOGNiMjBkYTMyYTgwNzQ5NDE4MmVmMDU3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUXxc8Mc6MmJ/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWUXxc8Mc6MmJ"
                }, {
                  "collaborative" : false,
                  "description" : "Some bops to take the pain of homework away.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX3csziQj0d5b"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3csziQj0d5b",
                  "id" : "37i9dQZF1DX3csziQj0d5b",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000264a2f100351022f13e2f8fa1",
                    "width" : null
                  } ],
                  "name" : "homework vibes",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMwMDQ4MCwwMDAwMDAwMGZiY2E3MmFkYjVjYzAxMmM5MGRlMDc5MDczNTJhZTQ2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3csziQj0d5b/tracks",
                    "total" : 150
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX3csziQj0d5b"
                }, {
                  "collaborative" : false,
                  "description" : "Be cool with refreshing Tropical K-Pop dance music! (시원청량한 국내 댄스곡을 즐겨보세요!) (Cover: NAYEON(나연))",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1lU51fgoMhF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1lU51fgoMhF",
                  "id" : "37i9dQZF1DX1lU51fgoMhF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000024e4a46a8c5d1749877435f1f",
                    "width" : null
                  } ],
                  "name" : "Summer K-Pop Hits (썸머 히트)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDIxMDkwNiwwMDAwMDAwMDM4YjlhNDk3OWVhZTE0YTkxZTk4ODE4Yzk2OTUwOTg2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1lU51fgoMhF/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1lU51fgoMhF"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-playlists%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 15
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Binspired-by-recent-playlists%5D",
              "id" : "home-personalized[inspired-by-recent-playlists]",
              "images" : [ ],
              "name" : "Based on your recent listening",
              "rendering" : "CAROUSEL",
              "tag_line" : "Inspired by your recent activity.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Bmore-of-what-you-like%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Chillout to the coolest Korean acoustic tunes. (Cover: 10cm) (감미롭고 부드러운 한국 어쿠스틱 음악과 함께하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1wdZM1FEz79"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79",
                  "id" : "37i9dQZF1DX1wdZM1FEz79",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002c33d807b43a03e0fceac8559",
                    "width" : null
                  } ],
                  "name" : "K-Pop Acoustic",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDEyNjI2NSwwMDAwMDAwMDQ0ZDE3YjQ5Njc3YzlkZGI0MjU1MjMyMTkyMmU3Y2Q4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79/tracks",
                    "total" : 52
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1wdZM1FEz79"
                }, {
                  "collaborative" : false,
                  "description" : "Listen to the soundtrack of the SBS Mon-Tue drama CHEER UP and the songs recommended by the cast! (SBS 월, 화 드라마 치얼업의 사운드 트랙과 배우진이 추천하는 노래를 들어보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX4Z5FphlroPF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4Z5FphlroPF",
                  "id" : "37i9dQZF1DX4Z5FphlroPF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000021aa954322c0dc7e0f00bb2a7",
                    "width" : null
                  } ],
                  "name" : "CHEER UP (치얼업)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3MDI5NzY4MywwMDAwMDAwMGE5MDk5MmFiOGIwMWZmNDAxY2VjN2E0MGI1MDk2Y2Q0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4Z5FphlroPF/tracks",
                    "total" : 33
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX4Z5FphlroPF"
                }, {
                  "collaborative" : false,
                  "description" : "Meet the vocalists representing Korea! (Cover:\u001DSURAN(수란)) (대한민국을 대표하는 보컬들을 만나보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8eqay1FtdMm"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8eqay1FtdMm",
                  "id" : "37i9dQZF1DX8eqay1FtdMm",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002ceb7cc20d85f60aa1435bc19",
                    "width" : null
                  } ],
                  "name" : "v o K a l",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjExMzM5MCwwMDAwMDAwMDM5NWE5YTM0NzliYTE2ODg3MTVjYWYwZDI1OWNlYmY3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8eqay1FtdMm/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8eqay1FtdMm"
                }, {
                  "collaborative" : false,
                  "description" : "Listen to the soundtracks of Disney+'s original drama Call It Love. (디즈니+ 오리지널 드라마 사랑이라 말해요의 사운드 트랙들을 즐겨보세요!) You can also listen to 15 songs personally selected by actor Kim Young-kwang. (김영광 배우가 직접 선정한 15곡도 들어보실 수 있습니다.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYJer66eoqCP"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYJer66eoqCP",
                  "id" : "37i9dQZF1DWYJer66eoqCP",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002f1728adc8dcc7ead01c0a756",
                    "width" : null
                  } ],
                  "name" : "사랑이라 말해요(Call It Love)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3OTkxMzk3MywwMDAwMDAwMGRiZTU2ZTY1MzM1NTkyODgwZjliYTk2NzYyODJmNTY4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYJer66eoqCP/tracks",
                    "total" : 28
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYJer66eoqCP"
                }, {
                  "collaborative" : false,
                  "description" : "Ready for some sweet harmony or explosive combination of your favourite couple or troublemakers. (Cover: SOLE, Sung Si Kyung)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWZYjbSZYSpu6"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZYjbSZYSpu6",
                  "id" : "37i9dQZF1DWZYjbSZYSpu6",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000023e12bbaa9eac9426deaae2b1",
                    "width" : null
                  } ],
                  "name" : "K-Pop Duets (러블리 듀엣)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzAwNzg4NiwwMDAwMDAwMDNjNDI0MTQ4N2QxNzZkODAxNDE1MGVlMjgxMDQ3YTY3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZYjbSZYSpu6/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWZYjbSZYSpu6"
                }, {
                  "collaborative" : false,
                  "description" : "Relive your favourite K-Drama moments with these classic soundtracks.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWUXxc8Mc6MmJ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUXxc8Mc6MmJ",
                  "id" : "37i9dQZF1DWUXxc8Mc6MmJ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000248af925fcc4cf111f8b1fc76",
                    "width" : null
                  } ],
                  "name" : "Best of Korean Soundtracks",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3ODY4NzQzNiwwMDAwMDAwMDAzMDllYTgzOGNiMjBkYTMyYTgwNzQ5NDE4MmVmMDU3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUXxc8Mc6MmJ/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWUXxc8Mc6MmJ"
                }, {
                  "collaborative" : false,
                  "description" : "Chill Korean tunes that's perfect with your latte or americano. (카페와 어울리는 편안한 음악들을 감상하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX5g856aiKiDS"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS",
                  "id" : "37i9dQZF1DX5g856aiKiDS",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000262a9874085e91a05440a1cee",
                    "width" : null
                  } ],
                  "name" : "Dalkom Cafe",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTU0ODE0NCwwMDAwMDAwMDgyZWFlZDJiMjU3ZGFhYTU3ZWE5YmVkNDRlODg5ODQ3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX5g856aiKiDS"
                }, {
                  "collaborative" : false,
                  "description" : "Enjoy the popular Korean Cyworld bgm that embroidered the 2000s and 2010s. (한 시절 감성을 대표했던 싸이월드 BGM을 감상해보세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXb64n6xan4nb"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXb64n6xan4nb",
                  "id" : "37i9dQZF1DXb64n6xan4nb",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002186387ecfcd99df288f576b5",
                    "width" : null
                  } ],
                  "name" : "CYWORLD BGM (싸이월드 BGM)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY1NTEyNjU2NiwwMDAwMDAwMDBhMWIyOGFlZTU1M2Y2NTBmNGZkYWJjMTgyYTllN2M1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXb64n6xan4nb/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXb64n6xan4nb"
                }, {
                  "collaborative" : false,
                  "description" : "Time to press play on these jaem jams from 2010 onwards! Cover: MONSTA X",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdR77H5Z8MIM"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM",
                  "id" : "37i9dQZF1DXdR77H5Z8MIM",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002dd04c8560daaa69bddfd25e6",
                    "width" : null
                  } ],
                  "name" : "Nolja!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjQ3NzI1OCwwMDAwMDAwMDBjYzY4YmVjY2Y0OWFkMWNkMDNjNThhZTdhODk3Njkw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM/tracks",
                    "total" : 70
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdR77H5Z8MIM"
                }, {
                  "collaborative" : false,
                  "description" : "K'ID = Korean ID. Welcome to the K-Rock anthem! (Cover: KyoungSeo(경서)) (핫한 국내 록 음악을 만나보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX2SFBzpAPi7n"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX2SFBzpAPi7n",
                  "id" : "37i9dQZF1DX2SFBzpAPi7n",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002f08068bc5bc805315dcd81e3",
                    "width" : null
                  } ],
                  "name" : "Cool K'IDs Rock",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTYzMTc5OSwwMDAwMDAwMGQ4OTMwZDhkOTc0NGZmN2Y0MDkxZjU4ZWY3MjYyMDc1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX2SFBzpAPi7n/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX2SFBzpAPi7n"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Bmore-of-what-you-like%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 20
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Bmore-of-what-you-like%5D",
              "id" : "home-personalized[more-of-what-you-like]",
              "images" : [ ],
              "name" : "More of what you like",
              "rendering" : "CAROUSEL",
              "tag_line" : "Hear a little bit of everything you love.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-personalized%5Brecommended-stations%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4x37mdfftaiA"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4x37mdfftaiA",
                  "id" : "37i9dQZF1E4x37mdfftaiA",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/0GsGBWIkeFJxFllGUemX5i/en",
                    "width" : null
                  } ],
                  "name" : "Jinyoung Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDNiMTAwZGFiYzcxYjM0MTdjNDZhZTNiZDZlZWUzYmU3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4x37mdfftaiA/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4x37mdfftaiA"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4jVSnSRXEfel"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4jVSnSRXEfel",
                  "id" : "37i9dQZF1E4jVSnSRXEfel",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/0ixzjrK1wkN2zWBXt3VW3W/en",
                    "width" : null
                  } ],
                  "name" : "Yuuri Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDkyYmY4NWZjZTI0MWFlNTk2MzFjMmE5YjEyOWI4YTUz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4jVSnSRXEfel/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4jVSnSRXEfel"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4pXr9gN0dGIQ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4pXr9gN0dGIQ",
                  "id" : "37i9dQZF1E4pXr9gN0dGIQ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/3HqSLMAZ3g3d5poNaI7GOU/en",
                    "width" : null
                  } ],
                  "name" : "IU Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDk3Y2QwYjJjN2MyMTk5YzY0MTUzMDljMTI1ODEyOGJh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4pXr9gN0dGIQ/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4pXr9gN0dGIQ"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4kjfJwQU1Pu4"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4kjfJwQU1Pu4",
                  "id" : "37i9dQZF1E4kjfJwQU1Pu4",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/3iRqbMhzyOyoCkmmMRxLWR/en",
                    "width" : null
                  } ],
                  "name" : "Hu Xia Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMGU2ODcxMTgxNTYyNzk2NzI2ZjU1NTdiYmE2YTQ0MTgx",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4kjfJwQU1Pu4/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4kjfJwQU1Pu4"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4t3F2zE384wZ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4t3F2zE384wZ",
                  "id" : "37i9dQZF1E4t3F2zE384wZ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/6rs1KAoQnFalSqSU4LTh8g/en",
                    "width" : null
                  } ],
                  "name" : "back number Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDE3NmZhMzU2MTllM2U3OWI0MzRkMzZiNjI1MmI0NGE2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4t3F2zE384wZ/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4t3F2zE384wZ"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4ovZPDXmdSLe"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4ovZPDXmdSLe",
                  "id" : "37i9dQZF1E4ovZPDXmdSLe",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/6jgrgDBt1SbtNbc25sLaTH/en",
                    "width" : null
                  } ],
                  "name" : "Busker Busker Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMGUzOGFiNjBkMTJhYmNlNjBkNDgzNTkyOGM5ZGJiMDU5",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4ovZPDXmdSLe/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4ovZPDXmdSLe"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4klJxC65VVfA"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4klJxC65VVfA",
                  "id" : "37i9dQZF1E4klJxC65VVfA",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/12AUp9oqeJDhNfO6IhQiNi/en",
                    "width" : null
                  } ],
                  "name" : "Lee Seung Gi Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDJkYjViNGE2ZmIwMTIwZGNmZTg2MmNhMjAzYmEzN2Ix",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4klJxC65VVfA/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4klJxC65VVfA"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4r6d68dy3IXY"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4r6d68dy3IXY",
                  "id" : "37i9dQZF1E4r6d68dy3IXY",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/7jFUYMpMUBDL4JQtMZ5ilc/en",
                    "width" : null
                  } ],
                  "name" : "Sung Si Kyung Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDc2YjA3OTZiOWYxYzgxZWE4MjU3ZmNjMGU5MTYzNWYx",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4r6d68dy3IXY/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4r6d68dy3IXY"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4wvsU3ufJWjf"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4wvsU3ufJWjf",
                  "id" : "37i9dQZF1E4wvsU3ufJWjf",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/706WzkJEacBrtkHKRpBU2q/en",
                    "width" : null
                  } ],
                  "name" : "Motohiro Hata Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMDYyZGMyNWY2NDQ0ODhmYzFjMmY1ZGJjZjg2MjU5ZjUz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4wvsU3ufJWjf/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4wvsU3ufJWjf"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1E4pDCq508Gwsm"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4pDCq508Gwsm",
                  "id" : "37i9dQZF1E4pDCq508Gwsm",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://seeded-session-images.scdn.co/v1/img/artist/7k73EtZwoPs516ZxE72KsO/en",
                    "width" : null
                  } ],
                  "name" : "ONE OK ROCK Radio",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjU0OTAzNSwwMDAwMDAwMGI2Y2FiMjE1YmE2Njc0ZTIwZTgyYjE1MjFmY2NlNTZl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1E4pDCq508Gwsm/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1E4pDCq508Gwsm"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-personalized%5Brecommended-stations%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 15
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-personalized%5Brecommended-stations%5D",
              "id" : "home-personalized[recommended-stations]",
              "images" : [ ],
              "name" : "Recommended radio",
              "rendering" : "CAROUSEL",
              "tag_line" : "Non-stop music based on your favorite songs and artists.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B0%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "De 50 populairste hits van Nederland. Cover: Kris Kross Amsterdam, Sofia  Reyes, Tinie Tempah",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSBi5svWQ9Nk"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSBi5svWQ9Nk",
                  "id" : "37i9dQZF1DWSBi5svWQ9Nk",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002e5740593ed7290bd0b3f9379",
                    "width" : null
                  } ],
                  "name" : "Hot Hits NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDc5ZmRjNDE5YmQwMDEyN2M1MDkzNjJiNGRiN2UwYmM4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSBi5svWQ9Nk/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSBi5svWQ9Nk"
                }, {
                  "collaborative" : false,
                  "description" : "De nieuwste releases elke vrijdag op Spotify! DYSTINCT",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXb5BKLTO7ULa"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXb5BKLTO7ULa",
                  "id" : "37i9dQZF1DXb5BKLTO7ULa",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000026ba0e866ec78fd5484998cbc",
                    "width" : null
                  } ],
                  "name" : "New Music Friday NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMwMzczNiwwMDAwMDAwMDdjZGM1MTNkMzNiMWY4ZTc4ODk1ODk1NDhlYTVkOTFl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXb5BKLTO7ULa/tracks",
                    "total" : 103
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXb5BKLTO7ULa"
                }, {
                  "collaborative" : false,
                  "description" : "De grootste Nederlandse hits van vroeger en nu. Cover: Metejoor & Hannah Mae",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdKMCnEhDnDL"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdKMCnEhDnDL",
                  "id" : "37i9dQZF1DXdKMCnEhDnDL",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002101a3c9b9d131a448e7209de",
                    "width" : null
                  } ],
                  "name" : "Beste van NL",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDEzOTYxMSwwMDAwMDAwMDYyMTFmM2UxMzg0M2VkZjUyNTBhYjc2OTc3ODMyM2Nh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdKMCnEhDnDL/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdKMCnEhDnDL"
                }, {
                  "collaborative" : false,
                  "description" : "Antoon & Ronnie Flex op de cover van de vernieuwde Je Moerstaal! ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWUX3x84bv557"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUX3x84bv557",
                  "id" : "37i9dQZF1DWUX3x84bv557",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b765703c296ecdb87795fc00",
                    "width" : null
                  } ],
                  "name" : "Je Moerstaal",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMDFhNmYwZmVmMTg4M2VjOTU3ODc1YmNiMWViNzYwZDUw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUX3x84bv557/tracks",
                    "total" : 90
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWUX3x84bv557"
                }, {
                  "collaborative" : false,
                  "description" : "Lil Durk & J. Cole are on top of the Hottest 50!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXcBWIGoYBM5M"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcBWIGoYBM5M",
                  "id" : "37i9dQZF1DXcBWIGoYBM5M",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000022ac21dacd63a52de41721848",
                    "width" : null
                  } ],
                  "name" : "Today's Top Hits ",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIwMCwwMDAwMDAwMDdhZDMyOWY0NTZhYjg5MTRiYzM2YWE5OGYwZjE0MTkw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcBWIGoYBM5M/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXcBWIGoYBM5M"
                }, {
                  "collaborative" : false,
                  "description" : "Altijd fris in Fresh Hits. Cover: Donnie & Chantal Janzen",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYrgs30Ir8ow"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYrgs30Ir8ow",
                  "id" : "37i9dQZF1DWYrgs30Ir8ow",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000020f266c1b4e947eaaa3f3d474",
                    "width" : null
                  } ],
                  "name" : "Fresh Hits",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI5NjUyOSwwMDAwMDAwMDM4OGRkOGI0MmQ4ODE4NDUwN2I1MjliMTRkZmU4ZmQ0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYrgs30Ir8ow/tracks",
                    "total" : 40
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYrgs30Ir8ow"
                }, {
                  "collaborative" : false,
                  "description" : "",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWMOmoXKqHTD"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWMOmoXKqHTD",
                  "id" : "37i9dQZF1DWWMOmoXKqHTD",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002ffa215be1a4c64e3cbf59d1e",
                    "width" : null
                  } ],
                  "name" : "Songs to Sing in the Car",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTcxMzgyMiwwMDAwMDAwMDQwNzJkZDQ5OGZmMTQ0MDQxOGQ3NjY4YzRjOWI3NDRk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWMOmoXKqHTD/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWMOmoXKqHTD"
                }, {
                  "collaborative" : false,
                  "description" : "Take it easy with these laid back tracks from the eighties...",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX6l1fwN15uV5"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6l1fwN15uV5",
                  "id" : "37i9dQZF1DX6l1fwN15uV5",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002478bac166066b4ef63733c3d",
                    "width" : null
                  } ],
                  "name" : "Easy 80s",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY0MjUwNzAxMCwwMDAwMDAwMGQ5ZGQ4ZTI1OTRlMGQ4ZTkxNTRiZDQ4ZmJhZWJjNzQy",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6l1fwN15uV5/tracks",
                    "total" : 80
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX6l1fwN15uV5"
                }, {
                  "collaborative" : false,
                  "description" : "Current favorites and exciting new music. Cover: BTS",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXcRXFNfZr7Tp"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcRXFNfZr7Tp",
                  "id" : "37i9dQZF1DXcRXFNfZr7Tp",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002d73b1b82468742cafc80a3e6",
                    "width" : null
                  } ],
                  "name" : "just hits",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzI2MCwwMDAwMDAwMDg3MjAxMmMzNGY5NDA3MDJiOGE0YjgxNGNmMWQ4YzI3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXcRXFNfZr7Tp/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXcRXFNfZr7Tp"
                }, {
                  "collaborative" : false,
                  "description" : "pov: geef me mijn jeugd terug",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXanDkFGa4syx"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXanDkFGa4syx",
                  "id" : "37i9dQZF1DXanDkFGa4syx",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002d7b6ab9070867f0849cddad3",
                    "width" : null
                  } ],
                  "name" : "Gen-Z Nostalgie",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTM5MzgyMCwwMDAwMDAwMDNkMmZmYmRiN2JlN2JjN2Y1ZjQxYjFmOWE1M2E5MTQ1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXanDkFGa4syx/tracks",
                    "total" : 200
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXanDkFGa4syx"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B0%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 12
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B0%5D",
              "id" : "ginger-genre-affinity[0]",
              "images" : [ ],
              "name" : "Pop",
              "rendering" : "CAROUSEL",
              "tag_line" : "Recommended for you.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B1%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Let's turn ON the movement! Cover: BTS",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX9tPFwDMOaN1"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9tPFwDMOaN1",
                  "id" : "37i9dQZF1DX9tPFwDMOaN1",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000021e0e387f00ba48958a196576",
                    "width" : null
                  } ],
                  "name" : "K-Pop ON! (온)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIwMCwwMDAwMDAwMGIxMmRmYWRhMGIxNzFhODc3MTQ5ZGU4NTg2ZmUyYmVm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9tPFwDMOaN1/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX9tPFwDMOaN1"
                }, {
                  "collaborative" : false,
                  "description" : "BLACKPINK in our area! Are you ready to Shut Down?",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8kP0ioXjxIA"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8kP0ioXjxIA",
                  "id" : "37i9dQZF1DX8kP0ioXjxIA",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000025f9620278c45065e4985e1fb",
                    "width" : null
                  } ],
                  "name" : "This Is BLACKPINK",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MDQ5OTQ3NSwwMDAwMDAwMGI3NzdmNTY1NTg2MTdhNTQ4NDM5NWQxYmI0NmE1ZDRi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8kP0ioXjxIA/tracks",
                    "total" : 48
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8kP0ioXjxIA"
                }, {
                  "collaborative" : false,
                  "description" : "This is the PROOF of BTS history. Check out <a href=\"https://open.spotify.com/playlist/37i9dQZF1DXaR2kf8OYllT\">BTS Yet To Come in Busan LIVE SET</a> as well. ARMY is unbeatable💜\n",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX08mhnhv6g9b"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX08mhnhv6g9b",
                  "id" : "37i9dQZF1DX08mhnhv6g9b",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002a7aec3961666dee881cee250",
                    "width" : null
                  } ],
                  "name" : "This Is BTS",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI5NTY0NCwwMDAwMDAwMGY3YzVkOWQwNjNiNTI3YmMyOGM5NjIxYTE1NzE2NDlk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX08mhnhv6g9b/tracks",
                    "total" : 244
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX08mhnhv6g9b"
                }, {
                  "collaborative" : false,
                  "description" : "Rolling with the 'bops' in your Kimbap. Bringing you the songs that are currently trending and everything else in between. Cover: (G)I-DLE",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX0018ciYu6bM"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0018ciYu6bM",
                  "id" : "37i9dQZF1DX0018ciYu6bM",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000021c235ba22856e51d2af81361",
                    "width" : null
                  } ],
                  "name" : "KimBops!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTUxMTY3MywwMDAwMDAwMDU1Y2JiNTllYmYxNTMwZmJkYWUyNWIzZTljMDE4Y2Zh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0018ciYu6bM/tracks",
                    "total" : 93
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX0018ciYu6bM"
                }, {
                  "collaborative" : false,
                  "description" : "Stray Kids everywhere all around the world. You make Stray Kids STAY. 5-STAR is here!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWqjEVD8TBr9"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWqjEVD8TBr9",
                  "id" : "37i9dQZF1DWWqjEVD8TBr9",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002c54519b4572f739116008e0f",
                    "width" : null
                  } ],
                  "name" : "This Is Stray Kids",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIzMSwwMDAwMDAwMDEzZGFjZDMyNTA3ZDg2N2FiYzhhNDI0MjBkNDUyMjU5",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWqjEVD8TBr9/tracks",
                    "total" : 160
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWqjEVD8TBr9"
                }, {
                  "collaborative" : false,
                  "description" : "From sultry vocalists, sexy divas to cutesy girl-groups, the women of K-Pop are a formidable force to be reckoned with. (Cover: MIRANI(미란이)) (케이팝을 대표하는 한국의 여성 가수들을 만나보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX6Cy4Vr7Hu2y"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6Cy4Vr7Hu2y",
                  "id" : "37i9dQZF1DX6Cy4Vr7Hu2y",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002027240b759f1c44ff0397c66",
                    "width" : null
                  } ],
                  "name" : "EQUAL K-Pop",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjA2MzYwMCwwMDAwMDAwMDRmMWJjZjc4YWI5YWM1NTdjZThmM2Q3ZGJhMWU2NWUz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6Cy4Vr7Hu2y/tracks",
                    "total" : 64
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX6Cy4Vr7Hu2y"
                }, {
                  "collaborative" : false,
                  "description" : "All your girl crushes(걸크러쉬) in one place. (Cover: DREAMCATCHER)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXbSWYCNwaARB"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXbSWYCNwaARB",
                  "id" : "37i9dQZF1DXbSWYCNwaARB",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000253dd657d6a4f981e8f980c3a",
                    "width" : null
                  } ],
                  "name" : "Girl Krush",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTAwMTI5OSwwMDAwMDAwMDNlOGQzZTdlM2RlNjNiN2MzODg5NDgyMzBiODQ4MWYz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXbSWYCNwaARB/tracks",
                    "total" : 40
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXbSWYCNwaARB"
                }, {
                  "collaborative" : false,
                  "description" : "Need to get your energy level up? This will help!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSnRSDTCsoPk"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSnRSDTCsoPk",
                  "id" : "37i9dQZF1DWSnRSDTCsoPk",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b50b2ffb67a9e0a1f9eef555",
                    "width" : null
                  } ],
                  "name" : "Energy Booster: K-Pop",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDQ3MjA3NiwwMDAwMDAwMDFlZTZlYjMyNjA4MTE1MWZkMmI3ODYwNTkwNzkyZTUz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSnRSDTCsoPk/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSnRSDTCsoPk"
                }, {
                  "collaborative" : false,
                  "description" : "The freshest K-Pop of today!  //最先端のK-Popをピックアップ！ Cover: Stray Kids",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1LU4UHKqdtg"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1LU4UHKqdtg",
                  "id" : "37i9dQZF1DX1LU4UHKqdtg",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002d7384baba783d97813597fbc",
                    "width" : null
                  } ],
                  "name" : "K-Pop Fresh",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjIzNzU4NSwwMDAwMDAwMDU0YTg1NzM0Y2U4ZTcyMWU3MjcwOWE2MDZhZGU2ZTdm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1LU4UHKqdtg/tracks",
                    "total" : 82
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1LU4UHKqdtg"
                }, {
                  "collaborative" : false,
                  "description" : "Say the name, Seventeen! Carats, get ready to slip into the diamond life with your 13 shining diamonds.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWXa2ShUct1Fm"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWXa2ShUct1Fm",
                  "id" : "37i9dQZF1DWXa2ShUct1Fm",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002caedcc09287f7b21bcefac51",
                    "width" : null
                  } ],
                  "name" : "This is SEVENTEEN",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjM5NDU0NSwwMDAwMDAwMDg0NjhjYjdjNTNkMWZiZWRjNTFiYzJiMDJjNWRlYTFm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWXa2ShUct1Fm/tracks",
                    "total" : 151
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWXa2ShUct1Fm"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B1%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 12
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B1%5D",
              "id" : "ginger-genre-affinity[1]",
              "images" : [ ],
              "name" : "K-Pop",
              "rendering" : "CAROUSEL",
              "tag_line" : "Recommended for you.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B2%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "De grootste dance hits van juni 2023. Cover: R3HAB & INNA",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWTwCImwcYjDL"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWTwCImwcYjDL",
                  "id" : "37i9dQZF1DWTwCImwcYjDL",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000207d2980b4e004efc3d7d9dc3",
                    "width" : null
                  } ],
                  "name" : "360 Dance",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI2MTYwMCwwMDAwMDAwMGIyZWQ3MzgwZWZkOGEyMjA3Y2ZjYmQ1MTExYWMyODZi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWTwCImwcYjDL/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWTwCImwcYjDL"
                }, {
                  "collaborative" : false,
                  "description" : "Feel-good <a href=\"spotify:genre:edm_dance\">dance music</a>!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSf2RDTDayIx"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSf2RDTDayIx",
                  "id" : "37i9dQZF1DWSf2RDTDayIx",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000225d50fa7cc51b307364050f5",
                    "width" : null
                  } ],
                  "name" : "Happy Beats",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIwMCwwMDAwMDAwMGEwNGQxNWMxNDRlNGY0OTM2MmRiNTM4MjdmYmNjMzZi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSf2RDTDayIx/tracks",
                    "total" : 150
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSf2RDTDayIx"
                }, {
                  "collaborative" : false,
                  "description" : "Welcome to the dark side of the club.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX6J5NfMJS675"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6J5NfMJS675",
                  "id" : "37i9dQZF1DX6J5NfMJS675",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000028103cb15cc2d6cfb861312fa",
                    "width" : null
                  } ],
                  "name" : "Techno Bunker",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMyNjQwMCwwMDAwMDAwMGJkMTAzYjhiMjc2YWYyMDNjMmI0ZWM3ZGRjN2JiNDBl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6J5NfMJS675/tracks",
                    "total" : 60
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX6J5NfMJS675"
                }, {
                  "collaborative" : false,
                  "description" : "Wekelijkse update! Op de cover: Lost Frequencies",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWrJKwf0q9nn"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWrJKwf0q9nn",
                  "id" : "37i9dQZF1DWWrJKwf0q9nn",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000028cf1a4076d5750c5438c2c4e",
                    "width" : null
                  } ],
                  "name" : "Hot New Dance",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMzMDM4NiwwMDAwMDAwMDE1NjFlNTE1MGNiZWFiMjAxNjU1ZjkyZDNmYWJmZjUz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWrJKwf0q9nn/tracks",
                    "total" : 75
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWrJKwf0q9nn"
                }, {
                  "collaborative" : false,
                  "description" : "Geef dat feestje herkenning met bekende hits in een remixjasje!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8EyMj5jl6Tz"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8EyMj5jl6Tz",
                  "id" : "37i9dQZF1DX8EyMj5jl6Tz",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000240b4d98e0c924bedc2d9c216",
                    "width" : null
                  } ],
                  "name" : "Remix Party 2023",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMzMDk1MiwwMDAwMDAwMDA2ZTQwYWY4YzFkNGJjNmZlODM1MDk3ZTk2NDI3MmUy",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8EyMj5jl6Tz/tracks",
                    "total" : 144
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8EyMj5jl6Tz"
                }, {
                  "collaborative" : false,
                  "description" : "the beat of your drift",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWY64wDtewQt"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWY64wDtewQt",
                  "id" : "37i9dQZF1DWWY64wDtewQt",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000025f25fc5f1dab8c8b0b8e63af",
                    "width" : null
                  } ],
                  "name" : "phonk",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMxODY5NiwwMDAwMDAwMGU4MjlkYzFlYmYxZGU0Zjc1YWJjOGUwYTdmYmQ0MmE1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWY64wDtewQt/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWY64wDtewQt"
                }, {
                  "collaborative" : false,
                  "description" : "Get ready for Ibiza season 2023. See you on the dancefloor.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXaCACvgOVs5K"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXaCACvgOVs5K",
                  "id" : "37i9dQZF1DXaCACvgOVs5K",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000217a53af149994f1ceb29d8c2",
                    "width" : null
                  } ],
                  "name" : "Ibiza 2023",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTcyMTAwMiwwMDAwMDAwMDE2OTNhOWZkZjBmMWI5ZTM5MjM3MGY3YzAyZTI4OGI0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXaCACvgOVs5K/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXaCACvgOVs5K"
                }, {
                  "collaborative" : false,
                  "description" : "Forget it and disappear with deep & melodic <a href=\"spotify:genre:edm_dance\">house</a>.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX2TRYkJECvfC"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX2TRYkJECvfC",
                  "id" : "37i9dQZF1DX2TRYkJECvfC",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000236b7c4ece12f6a254f41d5c1",
                    "width" : null
                  } ],
                  "name" : "Deep House Relax",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzI2MCwwMDAwMDAwMGMyNDZhNDA5NzA3M2U2OWY0ZTFiMDdiMmU4YmQ2NDJk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX2TRYkJECvfC/tracks",
                    "total" : 200
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX2TRYkJECvfC"
                }, {
                  "collaborative" : false,
                  "description" : "Check deze 'Feel good' soundtrack voor een goed humeur!!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWeNODNe68OF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWeNODNe68OF",
                  "id" : "37i9dQZF1DWWeNODNe68OF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000026aca34d6f22bdf3c88b9733f",
                    "width" : null
                  } ],
                  "name" : "Feeling Good, Feeling Great",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMwMzA0OSwwMDAwMDAwMGYwMGVhNzk5MTMzOTg0YmM4NGYwNDcxYTBhMWE4NmM0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWeNODNe68OF/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWeNODNe68OF"
                }, {
                  "collaborative" : false,
                  "description" : "House music lives here. United under one roof, featuring Jazzy.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXa8NOEUWPn9W"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXa8NOEUWPn9W",
                  "id" : "37i9dQZF1DXa8NOEUWPn9W",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002fbc30aec304c0aaf52bb1656",
                    "width" : null
                  } ],
                  "name" : "Housewerk",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIwMCwwMDAwMDAwMGI3OTMyNjk2ZmE5MjY1MGI2ZmQ5Mzk3ZmNjMzQwYWE0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXa8NOEUWPn9W/tracks",
                    "total" : 125
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXa8NOEUWPn9W"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B2%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 12
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/ginger-genre-affinity%5B2%5D",
              "id" : "ginger-genre-affinity[2]",
              "images" : [ ],
              "name" : "Electronic/Dance",
              "rendering" : "CAROUSEL",
              "tag_line" : "Recommended for you.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/focus-home-shelf?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Lo-fi beats voor extra concentratie\n",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWVRrbkzYIlbi"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWVRrbkzYIlbi",
                  "id" : "37i9dQZF1DWVRrbkzYIlbi",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002590587f6f57b7e131f15e513",
                    "width" : null
                  } ],
                  "name" : "Studeren Lo-Fi",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTM5Mzc2NiwwMDAwMDAwMDkwOTdmZjk0NTkwZjAxODQ1YmE0Y2YwMzExMGE1YTA1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWVRrbkzYIlbi/tracks",
                    "total" : 104
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWVRrbkzYIlbi"
                }, {
                  "collaborative" : false,
                  "description" : "Instrumentaal | Krijg alles gedaan met deze electronische muziek.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8sKnLLx9deI"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8sKnLLx9deI",
                  "id" : "37i9dQZF1DX8sKnLLx9deI",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002dc948f6166ed03af5cdf7ec5",
                    "width" : null
                  } ],
                  "name" : "To Do List",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMzMTA4NywwMDAwMDAwMDgyMzBmYTJkMGYzMTJlNDBkMzEyNWE4YjNmODZhYzU1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8sKnLLx9deI/tracks",
                    "total" : 82
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8sKnLLx9deI"
                }, {
                  "collaborative" : false,
                  "description" : "Muziek voor de optimale concentratie.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX0wdQKgYFb7Q"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0wdQKgYFb7Q",
                  "id" : "37i9dQZF1DX0wdQKgYFb7Q",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002a08f43c2095e41e28d783b94",
                    "width" : null
                  } ],
                  "name" : "Maximum Concentration",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDQ2ODg2MCwwMDAwMDAwMDk4MzJjZDY5MGVkZGQ1ODkwMTY0OWZiYjEwOTc3MTJm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX0wdQKgYFb7Q/tracks",
                    "total" : 250
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX0wdQKgYFb7Q"
                }, {
                  "collaborative" : false,
                  "description" : "Welcome to the soothing hum...",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWUZ5bk6qqDSy"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUZ5bk6qqDSy",
                  "id" : "37i9dQZF1DWUZ5bk6qqDSy",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002fb21c7706fe265bea8ec5a69",
                    "width" : null
                  } ],
                  "name" : "White Noise",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzYxODA1MiwwMDAwMDAwMDFmMDk3ZDQ0NDQzZDc5YmM2YmNkNzlhODEyOTQyYjk2",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWUZ5bk6qqDSy/tracks",
                    "total" : 246
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWUZ5bk6qqDSy"
                }, {
                  "collaborative" : false,
                  "description" : "Keep calm and focus with ambient and post-rock music.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWZeKCadgRdKQ"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZeKCadgRdKQ",
                  "id" : "37i9dQZF1DWZeKCadgRdKQ",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000025551996f500ba876bda73fa5",
                    "width" : null
                  } ],
                  "name" : "Deep Focus",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMyNDM2NCwwMDAwMDAwMDgyNTIyNDBjNTg5NmMzMGExMDU2MzBiOTBhMTM2N2Rj",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZeKCadgRdKQ/tracks",
                    "total" : 260
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWZeKCadgRdKQ"
                }, {
                  "collaborative" : false,
                  "description" : "chill beats, lofi vibes, new tracks every week... ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWQRwui0ExPn"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWQRwui0ExPn",
                  "id" : "37i9dQZF1DWWQRwui0ExPn",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002abaf6c3c6a4b29f8a4565a86",
                    "width" : null
                  } ],
                  "name" : "lofi beats",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjEzNzk0MiwwMDAwMDAwMGI1MDEwZjdlN2I4YTdkNTY4NzgxZjZkMWFlYjMzOGY1",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWQRwui0ExPn/tracks",
                    "total" : 750
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWQRwui0ExPn"
                }, {
                  "collaborative" : false,
                  "description" : "Simply rain ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8ymr6UES7vc"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8ymr6UES7vc",
                  "id" : "37i9dQZF1DX8ymr6UES7vc",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000024a46a7f4e55bbc386dc77f84",
                    "width" : null
                  } ],
                  "name" : "Rain Sounds",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY3NzUwODk2MiwwMDAwMDAwMDA4NTJkODI3MGQ5YWJkMjUyZTJhMWNmNDkzNWNkMGFl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8ymr6UES7vc/tracks",
                    "total" : 290
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8ymr6UES7vc"
                }, {
                  "collaborative" : false,
                  "description" : "Soothing, low frequencies for relaxation, meditation or sleep.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX4hpot8sYudB"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4hpot8sYudB",
                  "id" : "37i9dQZF1DX4hpot8sYudB",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002fbf8dcb03962d651152e64a4",
                    "width" : null
                  } ],
                  "name" : "Brown Noise",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjA4MDIxNCwwMDAwMDAwMGY2YjdiZTAyZjRmNGVlY2RhYzlkNzk4NjIzNDE1NWFi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX4hpot8sYudB/tracks",
                    "total" : 229
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX4hpot8sYudB"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/focus-home-shelf?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 73
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/focus-home-shelf",
              "id" : "focus-home-shelf",
              "images" : [ ],
              "name" : "Focus",
              "rendering" : "CAROUSEL",
              "tag_line" : "Music to help you concentrate.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/mood-home-wrapper?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "Koffie met gemoedelijke muziek op de achtergrond.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWYPwGkJoztcR"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYPwGkJoztcR",
                  "id" : "37i9dQZF1DWYPwGkJoztcR",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000027b57fc0419aa2933af2f9c6b",
                    "width" : null
                  } ],
                  "name" : "'t Koffiehuis",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjQxNzc2MiwwMDAwMDAwMDJmY2Y0Yzg5ZTEyOGFhZWQ0NTYwZWNiNDA2OWU2YThl",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWYPwGkJoztcR/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWYPwGkJoztcR"
                }, {
                  "collaborative" : false,
                  "description" : "Voel je goed met deze tijdloze Happy Tunes!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX9u7XXOp0l5L"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9u7XXOp0l5L",
                  "id" : "37i9dQZF1DX9u7XXOp0l5L",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002786e6c6e7e9e87db548ead41",
                    "width" : null
                  } ],
                  "name" : "Happy Tunes",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjU4ODY2MywwMDAwMDAwMDA5Y2JlNzI1MWZkMDAzZGYwOTdjMjMxYWZjMzc0Zjlh",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX9u7XXOp0l5L/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX9u7XXOp0l5L"
                }, {
                  "collaborative" : false,
                  "description" : "Check deze 'Feel good' soundtrack voor een goed humeur!!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWWeNODNe68OF"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWeNODNe68OF",
                  "id" : "37i9dQZF1DWWeNODNe68OF",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000026aca34d6f22bdf3c88b9733f",
                    "width" : null
                  } ],
                  "name" : "Feeling Good, Feeling Great",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjMwMzA0OSwwMDAwMDAwMGYwMGVhNzk5MTMzOTg0YmM4NGYwNDcxYTBhMWE4NmM0",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWWeNODNe68OF/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWWeNODNe68OF"
                }, {
                  "collaborative" : false,
                  "description" : "Lekker rustig aan doen op zondag met deze zachte pop liedjes.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWZpGSuzrdTXg"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZpGSuzrdTXg",
                  "id" : "37i9dQZF1DWZpGSuzrdTXg",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000025dff4961b72d9f7b52095550",
                    "width" : null
                  } ],
                  "name" : "Easy On Sunday",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTY1NjgwMCwwMDAwMDAwMDM0YTQ5OWExMzQ2MjFhMTcwMjQ1ZGQ5ZWFiNDMzNmIw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWZpGSuzrdTXg/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWZpGSuzrdTXg"
                }, {
                  "collaborative" : false,
                  "description" : "Feel-good <a href=\"spotify:genre:edm_dance\">dance music</a>!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSf2RDTDayIx"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSf2RDTDayIx",
                  "id" : "37i9dQZF1DWSf2RDTDayIx",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000225d50fa7cc51b307364050f5",
                    "width" : null
                  } ],
                  "name" : "Happy Beats",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI4MzIwMCwwMDAwMDAwMGEwNGQxNWMxNDRlNGY0OTM2MmRiNTM4MjdmYmNjMzZi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSf2RDTDayIx/tracks",
                    "total" : 150
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSf2RDTDayIx"
                }, {
                  "collaborative" : false,
                  "description" : "Take it easy with these laid back tracks from the eighties...",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX6l1fwN15uV5"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6l1fwN15uV5",
                  "id" : "37i9dQZF1DX6l1fwN15uV5",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002478bac166066b4ef63733c3d",
                    "width" : null
                  } ],
                  "name" : "Easy 80s",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY0MjUwNzAxMCwwMDAwMDAwMGQ5ZGQ4ZTI1OTRlMGQ4ZTkxNTRiZDQ4ZmJhZWJjNzQy",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX6l1fwN15uV5/tracks",
                    "total" : 80
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX6l1fwN15uV5"
                }, {
                  "collaborative" : false,
                  "description" : "Hits to boost your mood and fill you with happiness!",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdPec7aLTmlC"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdPec7aLTmlC",
                  "id" : "37i9dQZF1DXdPec7aLTmlC",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000024b0f8a04bbacfb4b0a3245df",
                    "width" : null
                  } ],
                  "name" : "Happy Hits!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTY1Njg2MCwwMDAwMDAwMDc3YmY3Nzc1MTEwZGU1ZjE0ODI1ZDllNzlkNzZhMTkw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdPec7aLTmlC/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdPec7aLTmlC"
                }, {
                  "collaborative" : false,
                  "description" : "Soak up these laid-back jams. ",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX83I5je4W4rP"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX83I5je4W4rP",
                  "id" : "37i9dQZF1DX83I5je4W4rP",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000219835d3cd3ba41a341e8ee4f",
                    "width" : null
                  } ],
                  "name" : "Beach Vibes",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjI3OTc0OSwwMDAwMDAwMGE1OTIyNjE4MDVlNzMxYTY1MDRkZDFkOTY0MGM5ZDVi",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX83I5je4W4rP/tracks",
                    "total" : 125
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX83I5je4W4rP"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/mood-home-wrapper?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 96
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/mood-home-wrapper",
              "id" : "mood-home-wrapper",
              "images" : [ ],
              "name" : "Mood",
              "rendering" : "CAROUSEL",
              "tag_line" : "Playlists to match your mood.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-discover-album-picks%5B0%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0rTP0x4vRFSDbhtqcCqc8K"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0rTP0x4vRFSDbhtqcCqc8K",
                    "id" : "0rTP0x4vRFSDbhtqcCqc8K",
                    "name" : "Ronghao Li",
                    "type" : "artist",
                    "uri" : "spotify:artist:0rTP0x4vRFSDbhtqcCqc8K"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/2FbkaXZl2bM5YU7sZYkL6Q"
                  },
                  "href" : "https://api.spotify.com/v1/albums/2FbkaXZl2bM5YU7sZYkL6Q",
                  "id" : "2FbkaXZl2bM5YU7sZYkL6Q",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27373b582424c9292623c0a6030",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0273b582424c9292623c0a6030",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485173b582424c9292623c0a6030",
                    "width" : 64
                  } ],
                  "name" : "縱橫四海",
                  "release_date" : "2022-12-21",
                  "release_date_precision" : "day",
                  "total_tracks" : 10,
                  "type" : "album",
                  "uri" : "spotify:album:2FbkaXZl2bM5YU7sZYkL6Q"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/2Wlh9x3DooFlQzHWmsxadh"
                    },
                    "href" : "https://api.spotify.com/v1/artists/2Wlh9x3DooFlQzHWmsxadh",
                    "id" : "2Wlh9x3DooFlQzHWmsxadh",
                    "name" : "GODA",
                    "type" : "artist",
                    "uri" : "spotify:artist:2Wlh9x3DooFlQzHWmsxadh"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/5wRKdJVo24MvrwOaT6Ftqs"
                  },
                  "href" : "https://api.spotify.com/v1/albums/5wRKdJVo24MvrwOaT6Ftqs",
                  "id" : "5wRKdJVo24MvrwOaT6Ftqs",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273c352c945c7809197e532b943",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02c352c945c7809197e532b943",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851c352c945c7809197e532b943",
                    "width" : 64
                  } ],
                  "name" : "고다 애니웨어 : 짱구는 못말려",
                  "release_date" : "2020-01-20",
                  "release_date_precision" : "day",
                  "total_tracks" : 4,
                  "type" : "album",
                  "uri" : "spotify:album:5wRKdJVo24MvrwOaT6Ftqs"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/6KsmQPHXE3qhzNNBPSZ0eB"
                    },
                    "href" : "https://api.spotify.com/v1/artists/6KsmQPHXE3qhzNNBPSZ0eB",
                    "id" : "6KsmQPHXE3qhzNNBPSZ0eB",
                    "name" : "Yoon Do Hyun",
                    "type" : "artist",
                    "uri" : "spotify:artist:6KsmQPHXE3qhzNNBPSZ0eB"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/55moFj46U2geTKCj35tZXE"
                  },
                  "href" : "https://api.spotify.com/v1/albums/55moFj46U2geTKCj35tZXE",
                  "id" : "55moFj46U2geTKCj35tZXE",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2733d742a1ee9159f2d26312fb1",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e023d742a1ee9159f2d26312fb1",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048513d742a1ee9159f2d26312fb1",
                    "width" : 64
                  } ],
                  "name" : "Singing Yoon Do Hyun",
                  "release_date" : "2014-01-01",
                  "release_date_precision" : "day",
                  "total_tracks" : 5,
                  "type" : "album",
                  "uri" : "spotify:album:55moFj46U2geTKCj35tZXE"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/66FF9LF0uO3W1zxEN0m8uN"
                    },
                    "href" : "https://api.spotify.com/v1/artists/66FF9LF0uO3W1zxEN0m8uN",
                    "id" : "66FF9LF0uO3W1zxEN0m8uN",
                    "name" : "Ronald Cheng",
                    "type" : "artist",
                    "uri" : "spotify:artist:66FF9LF0uO3W1zxEN0m8uN"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/0h7aIc9PofqkbcHvzumk2Y"
                  },
                  "href" : "https://api.spotify.com/v1/albums/0h7aIc9PofqkbcHvzumk2Y",
                  "id" : "0h7aIc9PofqkbcHvzumk2Y",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273c6c955dbfd6bf5f8f7507a40",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02c6c955dbfd6bf5f8f7507a40",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851c6c955dbfd6bf5f8f7507a40",
                    "width" : 64
                  } ],
                  "name" : "玩咗先至瞓",
                  "release_date" : "2021-04-22",
                  "release_date_precision" : "day",
                  "total_tracks" : 12,
                  "type" : "album",
                  "uri" : "spotify:album:0h7aIc9PofqkbcHvzumk2Y"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/57htMBtzpppc1yoXgjbslj"
                    },
                    "href" : "https://api.spotify.com/v1/artists/57htMBtzpppc1yoXgjbslj",
                    "id" : "57htMBtzpppc1yoXgjbslj",
                    "name" : "Park Hyo Shin",
                    "type" : "artist",
                    "uri" : "spotify:artist:57htMBtzpppc1yoXgjbslj"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/7l5z6PdgRUaww549kUVeGO"
                  },
                  "href" : "https://api.spotify.com/v1/albums/7l5z6PdgRUaww549kUVeGO",
                  "id" : "7l5z6PdgRUaww549kUVeGO",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27304f70644da711f17cb871303",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0204f70644da711f17cb871303",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485104f70644da711f17cb871303",
                    "width" : 64
                  } ],
                  "name" : "Gift E.C.H.O",
                  "release_date" : "2012-03-22",
                  "release_date_precision" : "day",
                  "total_tracks" : 15,
                  "type" : "album",
                  "uri" : "spotify:album:7l5z6PdgRUaww549kUVeGO"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/5tRk0bqMQubKAVowp35XtC"
                    },
                    "href" : "https://api.spotify.com/v1/artists/5tRk0bqMQubKAVowp35XtC",
                    "id" : "5tRk0bqMQubKAVowp35XtC",
                    "name" : "MC 張天賦",
                    "type" : "artist",
                    "uri" : "spotify:artist:5tRk0bqMQubKAVowp35XtC"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/34BPcfbDQkYaJLrCgrEwYx"
                  },
                  "href" : "https://api.spotify.com/v1/albums/34BPcfbDQkYaJLrCgrEwYx",
                  "id" : "34BPcfbDQkYaJLrCgrEwYx",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273efde350949d2ef3d410583cc",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02efde350949d2ef3d410583cc",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851efde350949d2ef3d410583cc",
                    "width" : 64
                  } ],
                  "name" : "This is MC",
                  "release_date" : "2023-01-20",
                  "release_date_precision" : "day",
                  "total_tracks" : 9,
                  "type" : "album",
                  "uri" : "spotify:album:34BPcfbDQkYaJLrCgrEwYx"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0LyfQWJT6nXafLPZqxe9Of"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0LyfQWJT6nXafLPZqxe9Of",
                    "id" : "0LyfQWJT6nXafLPZqxe9Of",
                    "name" : "Various Artists",
                    "type" : "artist",
                    "uri" : "spotify:artist:0LyfQWJT6nXafLPZqxe9Of"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/2otHALaMD90LSrFm5Vndun"
                  },
                  "href" : "https://api.spotify.com/v1/albums/2otHALaMD90LSrFm5Vndun",
                  "id" : "2otHALaMD90LSrFm5Vndun",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27307777bf4d7a4953d4f283416",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0207777bf4d7a4953d4f283416",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485107777bf4d7a4953d4f283416",
                    "width" : 64
                  } ],
                  "name" : "Begin Again Korea, Episode.7 (Original Television Soundtrack)",
                  "release_date" : "2020-08-10",
                  "release_date_precision" : "day",
                  "total_tracks" : 4,
                  "type" : "album",
                  "uri" : "spotify:album:2otHALaMD90LSrFm5Vndun"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/0Awqm7GXGiBp8fJNGvywra"
                    },
                    "href" : "https://api.spotify.com/v1/artists/0Awqm7GXGiBp8fJNGvywra",
                    "id" : "0Awqm7GXGiBp8fJNGvywra",
                    "name" : "理想混蛋",
                    "type" : "artist",
                    "uri" : "spotify:artist:0Awqm7GXGiBp8fJNGvywra"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4lNhQsVRJOBvSgBpdb6sXN"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4lNhQsVRJOBvSgBpdb6sXN",
                  "id" : "4lNhQsVRJOBvSgBpdb6sXN",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b27372c6a3908a663c6c7cdc17c6",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e0272c6a3908a663c6c7cdc17c6",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d0000485172c6a3908a663c6c7cdc17c6",
                    "width" : 64
                  } ],
                  "name" : "關掉 / 打開",
                  "release_date" : "2022-04-21",
                  "release_date_precision" : "day",
                  "total_tracks" : 10,
                  "type" : "album",
                  "uri" : "spotify:album:4lNhQsVRJOBvSgBpdb6sXN"
                }, {
                  "album_type" : "album",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/3rJbSZv98yyWLNOvD6MsAV"
                    },
                    "href" : "https://api.spotify.com/v1/artists/3rJbSZv98yyWLNOvD6MsAV",
                    "id" : "3rJbSZv98yyWLNOvD6MsAV",
                    "name" : "7!!",
                    "type" : "artist",
                    "uri" : "spotify:artist:3rJbSZv98yyWLNOvD6MsAV"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4Uqxq3iwghdOrbDWNOo0rd"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4Uqxq3iwghdOrbDWNOo0rd",
                  "id" : "4Uqxq3iwghdOrbDWNOo0rd",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b2731e80ff49846a5654e005d411",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e021e80ff49846a5654e005d411",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d000048511e80ff49846a5654e005d411",
                    "width" : 64
                  } ],
                  "name" : "アニップス",
                  "release_date" : "2016-03-09",
                  "release_date_precision" : "day",
                  "total_tracks" : 10,
                  "type" : "album",
                  "uri" : "spotify:album:4Uqxq3iwghdOrbDWNOo0rd"
                }, {
                  "album_type" : "single",
                  "artists" : [ {
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/artist/7ny6IxNWBrNrl3M0wRaboO"
                    },
                    "href" : "https://api.spotify.com/v1/artists/7ny6IxNWBrNrl3M0wRaboO",
                    "id" : "7ny6IxNWBrNrl3M0wRaboO",
                    "name" : "Sam Shore",
                    "type" : "artist",
                    "uri" : "spotify:artist:7ny6IxNWBrNrl3M0wRaboO"
                  } ],
                  "available_markets" : [ "AD", "AE", "AG", "AL", "AM", "AO", "AR", "AT", "AU", "AZ", "BA", "BB", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BN", "BO", "BR", "BS", "BT", "BW", "BY", "BZ", "CA", "CD", "CG", "CH", "CI", "CL", "CM", "CO", "CR", "CV", "CW", "CY", "CZ", "DE", "DJ", "DK", "DM", "DO", "DZ", "EC", "EE", "EG", "ES", "ET", "FI", "FJ", "FM", "FR", "GA", "GB", "GD", "GE", "GH", "GM", "GN", "GQ", "GR", "GT", "GW", "GY", "HK", "HN", "HR", "HT", "HU", "ID", "IE", "IL", "IN", "IQ", "IS", "IT", "JM", "JO", "JP", "KE", "KG", "KH", "KI", "KM", "KN", "KR", "KW", "KZ", "LA", "LB", "LC", "LI", "LK", "LR", "LS", "LT", "LU", "LV", "LY", "MA", "MC", "MD", "ME", "MG", "MH", "MK", "ML", "MN", "MO", "MR", "MT", "MU", "MV", "MW", "MX", "MY", "MZ", "NA", "NE", "NG", "NI", "NL", "NO", "NP", "NR", "NZ", "OM", "PA", "PE", "PG", "PH", "PK", "PL", "PS", "PT", "PW", "PY", "QA", "RO", "RS", "RW", "SA", "SB", "SC", "SE", "SG", "SI", "SK", "SL", "SM", "SN", "SR", "ST", "SV", "SZ", "TD", "TG", "TH", "TJ", "TL", "TN", "TO", "TR", "TT", "TV", "TW", "TZ", "UA", "UG", "US", "UY", "UZ", "VC", "VE", "VN", "VU", "WS", "XK", "ZA", "ZM", "ZW" ],
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/album/4G67X1Acr1hIc6dcsJLFps"
                  },
                  "href" : "https://api.spotify.com/v1/albums/4G67X1Acr1hIc6dcsJLFps",
                  "id" : "4G67X1Acr1hIc6dcsJLFps",
                  "images" : [ {
                    "height" : 640,
                    "url" : "https://i.scdn.co/image/ab67616d0000b273672f0ed3a5be241f29e580b3",
                    "width" : 640
                  }, {
                    "height" : 300,
                    "url" : "https://i.scdn.co/image/ab67616d00001e02672f0ed3a5be241f29e580b3",
                    "width" : 300
                  }, {
                    "height" : 64,
                    "url" : "https://i.scdn.co/image/ab67616d00004851672f0ed3a5be241f29e580b3",
                    "width" : 64
                  } ],
                  "name" : "Could Have Been Stardust",
                  "release_date" : "2020-12-11",
                  "release_date_precision" : "day",
                  "total_tracks" : 3,
                  "type" : "album",
                  "uri" : "spotify:album:4G67X1Acr1hIc6dcsJLFps"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-discover-album-picks%5B0%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 53
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-discover-album-picks%5B0%5D",
              "id" : "home-discover-album-picks[0]",
              "images" : [ ],
              "name" : "Album picks",
              "rendering" : "CAROUSEL",
              "tag_line" : "Albums for you based on what you like to listen to.",
              "type" : "view"
            }, {
              "content" : {
                "href" : "https://api.spotify.com/v1/views/home-editorial-collections%5B0JQ5IMCbQBLml5lAZcsbdA%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=0",
                "items" : [ {
                  "collaborative" : false,
                  "description" : "시원한 국내음악들과 드라이빙을 즐겨보세요! (Tap your feet to the K-Pop beats on your way through the driving moments.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX3sCx6B9EAOr"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3sCx6B9EAOr",
                  "id" : "37i9dQZF1DX3sCx6B9EAOr",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000201b578b93ab862c54d175cf5",
                    "width" : null
                  } ],
                  "name" : "드라이빙 댄스+ (K-Pop Driving)",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTcyNDUyOCwwMDAwMDAwMDhhNDNjNDhkMmYxMWFkYjJiMDQzMGU1NGJkNjYxNDdj",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3sCx6B9EAOr/tracks",
                    "total" : 150
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX3sCx6B9EAOr"
                }, {
                  "collaborative" : false,
                  "description" : "Workout to K-Pop? Count me in! (Cover: LE SSERAFIM) (신나는 케이팝 댄스 음악과 함께 운동을 즐겨보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX3ZeFHRhhi7Y"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3ZeFHRhhi7Y",
                  "id" : "37i9dQZF1DX3ZeFHRhhi7Y",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b0c585e8fdd157427b4fe4ed",
                    "width" : null
                  } ],
                  "name" : "WOR K  OUT",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzAxMjgwMCwwMDAwMDAwMGNlZTc2OWZlZWEzODAyOWE4YTkyZmIyOTM2YTA3MmJk",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX3ZeFHRhhi7Y/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX3ZeFHRhhi7Y"
                }, {
                  "collaborative" : false,
                  "description" : "Chill Korean tunes that's perfect with your latte or americano. (카페와 어울리는 편안한 음악들을 감상하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX5g856aiKiDS"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS",
                  "id" : "37i9dQZF1DX5g856aiKiDS",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f0000000262a9874085e91a05440a1cee",
                    "width" : null
                  } ],
                  "name" : "Dalkom Cafe",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NTU0ODE0NCwwMDAwMDAwMDgyZWFlZDJiMjU3ZGFhYTU3ZWE5YmVkNDRlODg5ODQ3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5g856aiKiDS/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX5g856aiKiDS"
                }, {
                  "collaborative" : false,
                  "description" : "Enjoy mysterious and dreamy music as if walking in a dream. (꿈 속을 거닐듯 신비롭고 몽환적인 음악들을 즐겨보세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8Z20Qthwz58"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8Z20Qthwz58",
                  "id" : "37i9dQZF1DX8Z20Qthwz58",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000026937248c9ef8c9f96dd2a110",
                    "width" : null
                  } ],
                  "name" : "d r e a m l i k e ㄲ ㅜ ㅁ",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzI3NDY1MiwwMDAwMDAwMGU5OGI0MjM5YTkwMzY2MWZlOWY4NDcxN2MwZWE5NmRm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8Z20Qthwz58/tracks",
                    "total" : 68
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8Z20Qthwz58"
                }, {
                  "collaborative" : false,
                  "description" : "벚꽃이 흩날리고 새싹이 피어오르는 봄의 설레임을 만끽하세요.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX5r2dSnnMHnG"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5r2dSnnMHnG",
                  "id" : "37i9dQZF1DX5r2dSnnMHnG",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f000000023c53e0635c196de9aa0be704",
                    "width" : null
                  } ],
                  "name" : "봄같은 설레임",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzI3NDI5NiwwMDAwMDAwMDdiOWVjMThiMWFlODQyNmNlZjBmNTIwNDlkNWU2YzZm",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX5r2dSnnMHnG/tracks",
                    "total" : 75
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX5r2dSnnMHnG"
                }, {
                  "collaborative" : false,
                  "description" : "Chillout to the coolest Korean acoustic tunes. (Cover: 10cm) (감미롭고 부드러운 한국 어쿠스틱 음악과 함께하세요.)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX1wdZM1FEz79"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79",
                  "id" : "37i9dQZF1DX1wdZM1FEz79",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002c33d807b43a03e0fceac8559",
                    "width" : null
                  } ],
                  "name" : "K-Pop Acoustic",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NDEyNjI2NSwwMDAwMDAwMDQ0ZDE3YjQ5Njc3YzlkZGI0MjU1MjMyMTkyMmU3Y2Q4",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX1wdZM1FEz79/tracks",
                    "total" : 52
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX1wdZM1FEz79"
                }, {
                  "collaborative" : false,
                  "description" : "새벽 감성. Issa Vibe.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXbShqaetC9Tw"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXbShqaetC9Tw",
                  "id" : "37i9dQZF1DXbShqaetC9Tw",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b1670ed57eb296260b0dccd5",
                    "width" : null
                  } ],
                  "name" : "4:00 AM GROOVE",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzI3NDAwMCwwMDAwMDAwMGYzNGM2MThhZjA3NGFkY2QzNzg4MDQ1YWFlZWU0Yzg5",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXbShqaetC9Tw/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXbShqaetC9Tw"
                }, {
                  "collaborative" : false,
                  "description" : "이불 안 속처럼 포근하고 편안한 음악들과 함께 폭신한 기분을 만끽하세요.",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DWSvk1AxYsbvo"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSvk1AxYsbvo",
                  "id" : "37i9dQZF1DWSvk1AxYsbvo",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002b2376c0ecee1d21c1ed19acb",
                    "width" : null
                  } ],
                  "name" : "포근 편안 폭신",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MzI3NDg1OCwwMDAwMDAwMDNmNDU2ZGE2YWU4YjM1MWNiMTk1OWU4MWJkMTFmY2Uz",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DWSvk1AxYsbvo/tracks",
                    "total" : 50
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DWSvk1AxYsbvo"
                }, {
                  "collaborative" : false,
                  "description" : "Meet the vocalists representing Korea! (Cover:\u001DSURAN(수란)) (대한민국을 대표하는 보컬들을 만나보세요!)",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DX8eqay1FtdMm"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8eqay1FtdMm",
                  "id" : "37i9dQZF1DX8eqay1FtdMm",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002ceb7cc20d85f60aa1435bc19",
                    "width" : null
                  } ],
                  "name" : "v o K a l",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4NjExMzM5MCwwMDAwMDAwMDM5NWE5YTM0NzliYTE2ODg3MTVjYWYwZDI1OWNlYmY3",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DX8eqay1FtdMm/tracks",
                    "total" : 100
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DX8eqay1FtdMm"
                }, {
                  "collaborative" : false,
                  "description" : "Time to press play on these jaem jams from 2010 onwards! Cover: MONSTA X",
                  "external_urls" : {
                    "spotify" : "https://open.spotify.com/playlist/37i9dQZF1DXdR77H5Z8MIM"
                  },
                  "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM",
                  "id" : "37i9dQZF1DXdR77H5Z8MIM",
                  "images" : [ {
                    "height" : null,
                    "url" : "https://i.scdn.co/image/ab67706f00000002dd04c8560daaa69bddfd25e6",
                    "width" : null
                  } ],
                  "name" : "Nolja!",
                  "owner" : {
                    "display_name" : "Spotify",
                    "external_urls" : {
                      "spotify" : "https://open.spotify.com/user/spotify"
                    },
                    "href" : "https://api.spotify.com/v1/users/spotify",
                    "id" : "spotify",
                    "type" : "user",
                    "uri" : "spotify:user:spotify"
                  },
                  "primary_color" : null,
                  "public" : null,
                  "snapshot_id" : "MTY4MjQ3NzI1OCwwMDAwMDAwMDBjYzY4YmVjY2Y0OWFkMWNkMDNjNThhZTdhODk3Njkw",
                  "tracks" : {
                    "href" : "https://api.spotify.com/v1/playlists/37i9dQZF1DXdR77H5Z8MIM/tracks",
                    "total" : 70
                  },
                  "type" : "playlist",
                  "uri" : "spotify:playlist:37i9dQZF1DXdR77H5Z8MIM"
                } ],
                "limit" : 10,
                "next" : "https://api.spotify.com/v1/views/home-editorial-collections%5B0JQ5IMCbQBLml5lAZcsbdA%5D?content_limit=10&locale=en_us&country=KR&timestamp=2023-06-12T05:50:35.699606479&types=track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album&limit=10&offset=10",
                "offset" : 0,
                "previous" : null,
                "total" : 37
              },
              "custom_fields" : { },
              "external_urls" : null,
              "href" : "https://api.spotify.com/v1/views/home-editorial-collections%5B0JQ5IMCbQBLml5lAZcsbdA%5D",
              "id" : "home-editorial-collections[0JQ5IMCbQBLml5lAZcsbdA]",
              "images" : [ ],
              "name" : "K-Pop Moods and Moments",
              "rendering" : "CAROUSEL",
              "tag_line" : null,
              "type" : "view"
            } ],
            "limit" : 20,
            "next" : null,
            "offset" : 0,
            "previous" : null,
            "total" : 20
          },
          "custom_fields" : { },
          "external_urls" : null,
          "href" : "https://api.spotify.com/v1/views/desktop-home",
          "id" : "desktop-home",
          "images" : [ ],
          "name" : "",
          "rendering" : "STACK",
          "tag_line" : null,
          "type" : "view"
        }
        """;

    public Task<IReadOnlyList<HomeGroup>> GetHomeViewAsync(string trackAlbumPlaylistPlaylistV2ArtistCollectionArtistCollectionAlbum, int limit,
        int contentLimit, CancellationToken none)
    {
        using var jsonDocument = JsonDocument.Parse(sample_json);

        return Task.FromResult(HomeGroup.ParseFrom(jsonDocument));
    }
}