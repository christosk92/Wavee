using System.Text.Json;
using Wavee.Core.Ids;
using Wavee.UI.Core.Contracts.Artist;

namespace Wavee.UI.Core.Sys.Mock;

internal sealed class MockArtistView : IArtistView
{
    public Task<SpotifyArtistView> GetArtistViewAsync(AudioId id, CancellationToken ct = default)
    {
        using var jsonDocu = JsonDocument.Parse(ARTIST_JSON);

        return Task.FromResult(SpotifyArtistView.From(jsonDocu, id));
    }

    private const string ARTIST_JSON = """
                {
          "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3",
          "info": {
            "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3",
            "name": "Michael Bublé",
            "portraits": [
              {
                "uri": "https://i.scdn.co/image/ab67616100005174ef8cf61fea4923d2bde68200"
              }
            ],
            "verified": true
          },
          "header_image": {
            "image": "https://i.scdn.co/image/ab67618600006fbaafa0cd7ea6c9f6a5859b6b84",
            "offset": 0
          },
          "top_tracks": {
            "tracks": [
              {
                "uri": "spotify:track:1AM8QdDFZMq6SrrqUnuQ9P",
                "playcount": 384478797,
                "name": "Feeling Good",
                "release": {
                  "uri": "spotify:album:1f9vWKabhNPNQnHLleExSh",
                  "name": "It's Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851c6ad2b2b62b581a23a7c1759"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:3lyLqIn8mybyEFTs8JJaLf",
                "playcount": 302127764,
                "name": "Home",
                "release": {
                  "uri": "spotify:album:1f9vWKabhNPNQnHLleExSh",
                  "name": "It's Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851c6ad2b2b62b581a23a7c1759"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:2ajUl8lBLAXOXNpG4NEPMz",
                "playcount": 189938626,
                "name": "Sway",
                "release": {
                  "uri": "spotify:album:3rpSksJSFdNFqk5vne8at2",
                  "name": "Michael Bublé",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851b732a522a686bb304a5d3fdf"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:4T6HLdP6OcAtqC6tGnQelG",
                "playcount": 285502692,
                "name": "Everything",
                "release": {
                  "uri": "spotify:album:3h4pyWRJIB9ZyRKXChbX22",
                  "name": "Call Me Irresponsible",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d000048512ceedc8c879a1f6784fbeef5"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:4fIWvT19w9PR0VVBuPYpWA",
                "playcount": 294956106,
                "name": "Haven't Met You Yet",
                "release": {
                  "uri": "spotify:album:3MXDonOIzrIrCh0HvlACyj",
                  "name": "Crazy Love",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851f0cc194252888c6658c706ab"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:0mvkwaZMP2gAy2ApQLtZRv",
                "playcount": 165962118,
                "name": "It's a Beautiful Day",
                "release": {
                  "uri": "spotify:album:4Yf5LJfqpjgl1a4TBiCi07",
                  "name": "To Be Loved",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851051ae642ad4a0c1329b41d99"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:4m4aQLiPbWFyE07xRoJPhc",
                "playcount": 28728607,
                "name": "L O V E",
                "release": {
                  "uri": "spotify:album:5dXU3rx7fHkNJ1kTzvRQb3",
                  "name": "Call Me Irresponsible (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851d72cafc06acc89d30bf6093a"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:0JCfVb2qlq79Q479BUb0jK",
                "playcount": 49486872,
                "name": "Alone Again (Naturally)",
                "release": {
                  "uri": "spotify:album:6xUodRTpBiWXfQwPVZ5hIN",
                  "name": "Wallflower",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00004851e92ff8f74f4ee9efb4587795"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:4bEpL49l4f0r8GtsjUFUqL",
                "playcount": 17645649,
                "name": "Higher",
                "release": {
                  "uri": "spotify:album:6b6xEoiubMlgeGN6nrWM2V",
                  "name": "Higher",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d0000485193f378a9a1bed50491d01fb9"
                  }
                },
                "explicit": false
              },
              {
                "uri": "spotify:track:7JEUg9KqmpdIE5Nbb9ss66",
                "playcount": 109003659,
                "name": "Love You Anymore",
                "release": {
                  "uri": "spotify:album:68xKnVblFsSQ48CtgZT0oY",
                  "name": "love (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d000048515f3f20826d44c30a017fd68e"
                  }
                },
                "explicit": false
              }
            ]
          },
          "related_artists": {
            "artists": [
              {
                "uri": "spotify:artist:6u17YlWtW4oqFF5Hn9UU79",
                "name": "Harry Connick, Jr.",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051745246fd018fecb5f3cce9d906"
                  }
                ]
              },
              {
                "uri": "spotify:artist:4sj6D0zlMOl25nprDJBiU9",
                "name": "Andy Williams",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051745888acdca5e748e796b4e69b"
                  }
                ]
              },
              {
                "uri": "spotify:artist:2lolQgalUvZDfp5vvVtTYV",
                "name": "Tony Bennett",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174cd9641c8b449440a77ee19cc"
                  }
                ]
              },
              {
                "uri": "spotify:artist:7v4imS0moSyGdXyLgVTIV7",
                "name": "Nat King Cole",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051742be5d8fd3746a70e9637a665"
                  }
                ]
              },
              {
                "uri": "spotify:artist:5v8jlSmAQfrkTjAlpUfWtu",
                "name": "Perry Como",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/31a7808b0d354a5254762827ebfd2b33ac0ab12d"
                  }
                ]
              },
              {
                "uri": "spotify:artist:49e4v89VmlDcFCMyDv9wQ9",
                "name": "Dean Martin",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051742c21cafe54e02803fa5705e0"
                  }
                ]
              },
              {
                "uri": "spotify:artist:2UPnuV7os71xTZTyyEgj1B",
                "name": "Steve Tyrell",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051746e7893ef4a8833b8eacb74fb"
                  }
                ]
              },
              {
                "uri": "spotify:artist:0EodhzA6yW1bIdD5B4tcmJ",
                "name": "Bobby Darin",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/0145d946866cb2205bba2f17ab445e290eb1ec4c"
                  }
                ]
              },
              {
                "uri": "spotify:artist:3XxxEq6BREC57nCWXbQZ7o",
                "name": "Jamie Cullum",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051743197aa1b6fa7d0359656de10"
                  }
                ]
              },
              {
                "uri": "spotify:artist:5z1VAFwT35EVvCp1XlZZuL",
                "name": "Diana Krall",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174d1bfc97e11bb4c1f90f287a6"
                  }
                ]
              },
              {
                "uri": "spotify:artist:0MHgLfmQdutffmvWe5XBTN",
                "name": "Burl Ives",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174cec2dd52046443079ba66472"
                  }
                ]
              },
              {
                "uri": "spotify:artist:602DnpaSXJB4b9DZrvxbDc",
                "name": "Peggy Lee",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab6761610000517485605d6c52137d86e152f07f"
                  }
                ]
              },
              {
                "uri": "spotify:artist:19B0pJt4UEl3fUijGTRzxB",
                "name": "Renee Olstead",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174220f0f5a9f3166bab303e9a4"
                  }
                ]
              },
              {
                "uri": "spotify:artist:21LGsW7bziR4Ledx7WZ1Wf",
                "name": "Johnny Mathis",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174c388621eabac860a2db9af71"
                  }
                ]
              },
              {
                "uri": "spotify:artist:6cXMpsP9x0SH4kFfMyVezF",
                "name": "Josh Groban",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab67616100005174e092736f3d1d2e5ab393c6c7"
                  }
                ]
              },
              {
                "uri": "spotify:artist:1Mxqyy3pSjf8kZZL4QVxS0",
                "name": "Frank Sinatra",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/883de3e492364891543bc0313ffe516626778a16"
                  }
                ]
              },
              {
                "uri": "spotify:artist:5V0MlUE1Bft0mbLlND7FJz",
                "name": "Ella Fitzgerald",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab7d0c038d876a9ed5a21afb83d6ba760430cf90"
                  }
                ]
              },
              {
                "uri": "spotify:artist:5ixB75BQR3ADoWQkcHQJTs",
                "name": "Gene Autry",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/66a7fe2b2fd9388cf860f88e8636910f161be6cd"
                  }
                ]
              },
              {
                "uri": "spotify:artist:6ZjFtWeHP9XN7FeKSUe80S",
                "name": "Bing Crosby",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051740ba8db83b01de120a6f2312c"
                  }
                ]
              },
              {
                "uri": "spotify:artist:4cPHsZM98sKzmV26wlwD2W",
                "name": "Brenda Lee",
                "portraits": [
                  {
                    "uri": "https://i.scdn.co/image/ab676161000051744ea49bdca366a3baa5cbb006"
                  }
                ]
              }
            ]
          },
          "biography": {
            "text": "Michael Bublé was a man on a mission when he signed his record deal with Reprise Records almost two decades ago.\n \nFirst, he made a vow to himself to not only keep the flames of the great classics of the American Songbook alive and well - to not only breathe new life into them - but to bring his singular style, vocal power and passion to these timeless tunes that he loved.  \n \nSecondly, he was going to write number one pop hits that would become classics unto themselves. \n \nThird and most crucial for him was to bring all this music together in concert and take his audiences on a special journey - to give them an evening they would never forget.\n \nEvery night, he would sing his heart out, serenade them with beautiful love songs, make them laugh, cry, dance and enthrall sold out crowds in arenas and stadiums in countries around the world. He wanted his shows to be legendary. Clearly he has succeeded on all these fronts, even beyond his own wildest dreams.  The multi-platinum singer/ songwriter/producer/humanitarian is officially a global phenomenon. \n"
          },
          "releases": {
            "albums": {
              "releases": [
                {
                  "uri": "spotify:album:6b6xEoiubMlgeGN6nrWM2V",
                  "name": "Higher",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0293f378a9a1bed50491d01fb9"
                  },
                  "year": 2022,
                  "track_count": 13,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:0LJIVYOer8mlCmTrKoP9Kh",
                          "playcount": 22865512,
                          "name": "I'll Never Not Love You",
                          "popularity": 54,
                          "number": 1,
                          "duration": 218390,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3xZFuVIvzqBImEYJydRkp9",
                          "playcount": 5142858,
                          "name": "My Valentine",
                          "popularity": 44,
                          "number": 2,
                          "duration": 208865,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1ugWQPtI7SNYDXqEwuEjVm",
                          "playcount": 4358038,
                          "name": "A Nightingale Sang in Berkeley Square",
                          "popularity": 49,
                          "number": 3,
                          "duration": 185531,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4BECSdUBaiMX0dNmLHr0kZ",
                          "playcount": 3899918,
                          "name": "Make You Feel My Love",
                          "popularity": 51,
                          "number": 4,
                          "duration": 197754,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7zqznhNk8DZbwo8fWIF82V",
                          "playcount": 2791144,
                          "name": "Baby I'll Wait",
                          "popularity": 51,
                          "number": 5,
                          "duration": 143386,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4bEpL49l4f0r8GtsjUFUqL",
                          "playcount": 17645649,
                          "name": "Higher",
                          "popularity": 54,
                          "number": 6,
                          "duration": 185388,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1TRrLxSnUU82IknmBVARj5",
                          "playcount": 1547643,
                          "name": "Crazy (with Willie Nelson)",
                          "popularity": 42,
                          "number": 7,
                          "duration": 294196,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Willie Nelson",
                              "uri": "spotify:artist:5W5bDNCqJ1jbCgTxDD0Cb3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5bVFeC0hVdaA29A1V6hIqY",
                          "playcount": 1790644,
                          "name": "Bring It On Home to Me",
                          "popularity": 42,
                          "number": 8,
                          "duration": 275817,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4vTqXqjBzpkQDMdj8PNKXb",
                          "playcount": 1162646,
                          "name": "Don't Get Around Much Anymore",
                          "popularity": 39,
                          "number": 9,
                          "duration": 203506,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0breJW7kUS0qXn4tn2w7as",
                          "playcount": 1174107,
                          "name": "Mother",
                          "popularity": 40,
                          "number": 10,
                          "duration": 237078,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:42EFrDhJpTFQqDyYQOaChn",
                          "playcount": 988343,
                          "name": "Don't Take Your Love From Me",
                          "popularity": 38,
                          "number": 11,
                          "duration": 236456,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2S1zVDTIjxu6267HnoOOkz",
                          "playcount": 2922283,
                          "name": "You're the First, the Last, My Everything",
                          "popularity": 52,
                          "number": 12,
                          "duration": 225416,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1bV7gUR0CfAOnqro4vOS5U",
                          "playcount": 2109018,
                          "name": "Smile",
                          "popularity": 42,
                          "number": 13,
                          "duration": 227596,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 3,
                  "day": 25
                },
                {
                  "uri": "spotify:album:1jaa44Iw2iHhon2TRLLLoz",
                  "name": "The Essential Michael Bublé",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0269a866edcc445b20c033e0c5"
                  },
                  "year": 2022,
                  "track_count": 14,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:5dOn3nEm8JYINH5iAZQMhT",
                          "playcount": 22865512,
                          "name": "I'll Never Not Love You",
                          "popularity": 35,
                          "number": 1,
                          "duration": 218390,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5yyMkhLOhEzG7lfCHR5L0c",
                          "playcount": 384478797,
                          "name": "Feeling Good",
                          "popularity": 38,
                          "number": 2,
                          "duration": 237029,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5Cn5slkG8DAt5ctc1CkGT1",
                          "playcount": 49249664,
                          "name": "Me and Mrs. Jones",
                          "popularity": 34,
                          "number": 3,
                          "duration": 273819,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0VMZxc2K9VFfSEjN7UIm0y",
                          "playcount": 189938626,
                          "name": "Sway",
                          "popularity": 37,
                          "number": 4,
                          "duration": 188866,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3nuqOkf6F28JjXV4CZS7Nz",
                          "playcount": 302127764,
                          "name": "Home",
                          "popularity": 35,
                          "number": 5,
                          "duration": 226340,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2gWkOpa4fXWBi25kIKs0eO",
                          "playcount": 17610163,
                          "name": "Moondance",
                          "popularity": 33,
                          "number": 6,
                          "duration": 254133,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6hDG0PRlivxGPXPsU6sQlA",
                          "playcount": 294956106,
                          "name": "Haven't Met You Yet",
                          "popularity": 35,
                          "number": 7,
                          "duration": 245114,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:15XcKVYDKc2yganqpfdUU7",
                          "playcount": 285502692,
                          "name": "Everything",
                          "popularity": 34,
                          "number": 8,
                          "duration": 213266,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3RvZy0LTNLdArFGt9Kdd4r",
                          "playcount": 18524669,
                          "name": "Fever",
                          "popularity": 33,
                          "number": 9,
                          "duration": 231653,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6mIqwvGI5rXkpZ5KNbhdcC",
                          "playcount": 44658872,
                          "name": "Cry Me a River",
                          "popularity": 34,
                          "number": 10,
                          "duration": 254760,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:05ahod9jZPWjU3sdH3cgtE",
                          "playcount": 17645649,
                          "name": "Higher",
                          "popularity": 33,
                          "number": 11,
                          "duration": 185388,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:56sVPxIxM2tpUMlJJagRcT",
                          "playcount": 39407148,
                          "name": "Hold On",
                          "popularity": 31,
                          "number": 12,
                          "duration": 246284,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4QjXd6Pd0KpdQBozpqUq7V",
                          "playcount": 67529586,
                          "name": "Save the Last Dance for Me",
                          "popularity": 33,
                          "number": 13,
                          "duration": 218683,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0q5u1lL7X0rfMHJKlXluMn",
                          "playcount": 4424115,
                          "name": "Dream",
                          "popularity": 30,
                          "number": 14,
                          "duration": 306761,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 3,
                  "day": 25
                },
                {
                  "uri": "spotify:album:0FHpjWlnUmplF5ciL84Wpa",
                  "name": "Christmas (Deluxe 10th Anniversary Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020211d806261a42bf2bb7ebd7"
                  },
                  "year": 2021,
                  "track_count": 25,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:3SwZekRsxV4zUSBHc3nlRz",
                          "playcount": 806727841,
                          "name": "It's Beginning to Look a Lot like Christmas",
                          "popularity": 35,
                          "number": 1,
                          "duration": 207199,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5rd2xZdxxgWvpiCAeiKqTp",
                          "playcount": 252103167,
                          "name": "Santa Claus Is Coming to Town",
                          "popularity": 32,
                          "number": 2,
                          "duration": 171806,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6TsBYxKlER3Lf9AU2YDpWd",
                          "playcount": 185792518,
                          "name": "Jingle Bells (feat. The Puppini Sisters)",
                          "popularity": 30,
                          "number": 3,
                          "duration": 161804,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "The Puppini Sisters",
                              "uri": "spotify:artist:1svaANJTE5KrG16fTGDqOs"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1yPHgpNbt3veEinJa3AKPs",
                          "playcount": 167871409,
                          "name": "White Christmas (with Shania Twain)",
                          "popularity": 30,
                          "number": 4,
                          "duration": 218465,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Shania Twain",
                              "uri": "spotify:artist:5e4Dhzv426EvQe3aDb64jL"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2h9xjIaDX8vRWCW2eAfFv2",
                          "playcount": 166461897,
                          "name": "All I Want for Christmas Is You",
                          "popularity": 31,
                          "number": 5,
                          "duration": 174085,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0be2886aeZyXqWs8RYxhbt",
                          "playcount": 475876500,
                          "name": "Holly Jolly Christmas",
                          "popularity": 31,
                          "number": 6,
                          "duration": 121526,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6pvBPOZRXawJyUSMhKe69Q",
                          "playcount": 112760472,
                          "name": "Santa Baby",
                          "popularity": 28,
                          "number": 7,
                          "duration": 232949,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6Gc3SLkhqBDeIbEaXC1z3c",
                          "playcount": 142898770,
                          "name": "Have Yourself a Merry Little Christmas",
                          "popularity": 30,
                          "number": 8,
                          "duration": 232106,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3grVxsPu2nUtbXzdlT59PN",
                          "playcount": 145306957,
                          "name": "Christmas (Baby Please Come Home)",
                          "popularity": 30,
                          "number": 9,
                          "duration": 189794,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2XqwIvr8nxKKvXw8HvG1pN",
                          "playcount": 103141388,
                          "name": "Silent Night",
                          "popularity": 28,
                          "number": 10,
                          "duration": 230059,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5gPYm4laIMr60qoHHiXVDr",
                          "playcount": 93492085,
                          "name": "Blue Christmas",
                          "popularity": 26,
                          "number": 11,
                          "duration": 223927,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3qPwgqZV0Nce3IULH1dGMs",
                          "playcount": 141134037,
                          "name": "Cold December Night",
                          "popularity": 27,
                          "number": 12,
                          "duration": 200704,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2PyewGjlnz9oLgsbwnQVB5",
                          "playcount": 168844196,
                          "name": "I'll Be Home for Christmas",
                          "popularity": 28,
                          "number": 13,
                          "duration": 267608,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1T1fH7YfGvRUDYERtBXPsz",
                          "playcount": 72311165,
                          "name": "Ave Maria",
                          "popularity": 24,
                          "number": 14,
                          "duration": 243031,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6cthJMrE5t3zUPRxUtj61w",
                          "playcount": 82230153,
                          "name": "Mis Deseos / Feliz Navidad (with Thalia)",
                          "popularity": 25,
                          "number": 15,
                          "duration": 264878,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Thalia",
                              "uri": "spotify:artist:23wEWD21D4TPYiJugoXmYb"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3x0wbrv15rqDu0cmA3HGyX",
                          "playcount": 68065136,
                          "name": "The Christmas Song (Chestnuts Roasting on an Open Fire)",
                          "popularity": 27,
                          "number": 16,
                          "duration": 260095,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4gwNVxuyiCL0pPG2MIXc1A",
                          "playcount": 79854686,
                          "name": "Winter Wonderland",
                          "popularity": 27,
                          "number": 17,
                          "duration": 150880,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:024fbp2u8HMfZhXrVvbRbN",
                          "playcount": 70673273,
                          "name": "Frosty the Snowman (feat. The Puppini Sisters)",
                          "popularity": 25,
                          "number": 18,
                          "duration": 162029,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "The Puppini Sisters",
                              "uri": "spotify:artist:1svaANJTE5KrG16fTGDqOs"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2dnNCViz7wU5FclmN5KcT2",
                          "playcount": 54874356,
                          "name": "Silver Bells (feat. Naturally 7)",
                          "popularity": 24,
                          "number": 19,
                          "duration": 189281,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Naturally 7",
                              "uri": "spotify:artist:769D3IwCDrdPospAd3Hlpi"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3fAwHosupbXXkj4EJShzbz",
                          "playcount": 37945382,
                          "name": "White Christmas",
                          "popularity": 26,
                          "number": 20,
                          "duration": 202890,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2EmEXCi8ue7p5Xqmqz10kW",
                          "playcount": 34586482,
                          "name": "Let It Snow! - 10th Anniversary",
                          "popularity": 34,
                          "number": 21,
                          "duration": 159629,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:57aO285CKSQ7p6tTKKsDXa",
                          "playcount": 25786739,
                          "name": "Winter Wonderland (feat. Rod Stewart)",
                          "popularity": 25,
                          "number": 22,
                          "duration": 144040,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Rod Stewart",
                              "uri": "spotify:artist:2y8Jo9CKhJvtfeKOsYzRdT"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:30QRKfHBopZn6UNqqSJQ9O",
                          "playcount": 21033089,
                          "name": "The Christmas Sweater",
                          "popularity": 33,
                          "number": 23,
                          "duration": 223671,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3SmnL4d9GfHMe9Sr637bC5",
                          "playcount": 26492159,
                          "name": "The More You Give (The More You'll Have)",
                          "popularity": 25,
                          "number": 24,
                          "duration": 191002,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0b9DBC29UYwu35DeQa6vlJ",
                          "playcount": 17389,
                          "name": "Michael's Christmas Greeting",
                          "popularity": 1,
                          "number": 25,
                          "duration": 8191,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 11,
                  "day": 19
                },
                {
                  "uri": "spotify:album:7f14q8bSMN3168ifOhmy3M",
                  "name": "Bublé! (Original Soundtrack from his NBC TV Special)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fba4d4f37bc4ba2633264d1d"
                  },
                  "year": 2019,
                  "track_count": 9,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:2fvoYcBPkisbCwlXbkd2Ob",
                          "playcount": 16135898,
                          "name": "When You're Smiling",
                          "popularity": 42,
                          "number": 1,
                          "duration": 201539,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:564R0qcMyJD5WtRQDScTm4",
                          "playcount": 7112430,
                          "name": "Fly Me to the Moon / You're Nobody till Somebody Loves You / Just a Gigolo / Fly Me to the Moon (Reprise)",
                          "popularity": 43,
                          "number": 2,
                          "duration": 371125,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6bfmn9xi67kQBOaIgwQK6r",
                          "playcount": 3094638,
                          "name": "When I Fall in Love",
                          "popularity": 42,
                          "number": 3,
                          "duration": 187429,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1aZy7glG4tL1Uj3uX8kKtC",
                          "playcount": 1992549,
                          "name": "My Funny Valentine",
                          "popularity": 30,
                          "number": 4,
                          "duration": 144294,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5J9wGthdJp5mib0x6MuzkA",
                          "playcount": 2930176,
                          "name": "La vie en rose (feat. Cécile McLorin Salvant)",
                          "popularity": 37,
                          "number": 5,
                          "duration": 194040,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Cécile McLorin Salvant",
                              "uri": "spotify:artist:6PkSULcbxFKkxdgrmPGAvn"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2JbL9ZTk8RyUgSpcbfnold",
                          "playcount": 2816645,
                          "name": "It's a Beautiful Day / Haven't Met You Yet / Home",
                          "popularity": 35,
                          "number": 6,
                          "duration": 299856,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3hNwxLTfEu9B9s2xMAAIzT",
                          "playcount": 2119829,
                          "name": "Such a Night",
                          "popularity": 32,
                          "number": 7,
                          "duration": 201038,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3sMfOB34oxgUWoGUoOeBdZ",
                          "playcount": 3181728,
                          "name": "Feeling Good",
                          "popularity": 38,
                          "number": 8,
                          "duration": 235527,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1aTPFvd1gSIbzfVbo8zFMp",
                          "playcount": 2360548,
                          "name": "A Song for You",
                          "popularity": 33,
                          "number": 9,
                          "duration": 229630,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 3,
                  "day": 21
                },
                {
                  "uri": "spotify:album:68xKnVblFsSQ48CtgZT0oY",
                  "name": "love (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025f3f20826d44c30a017fd68e"
                  },
                  "year": 2018,
                  "track_count": 13,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:2b7XO47o117TqSfUd4kTQT",
                          "playcount": 35174642,
                          "name": "When I Fall in Love",
                          "popularity": 53,
                          "number": 1,
                          "duration": 243507,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3Rth37LIX8nTYyT1XskmFR",
                          "playcount": 22877513,
                          "name": "I Only Have Eyes for You",
                          "popularity": 49,
                          "number": 2,
                          "duration": 202854,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7JEUg9KqmpdIE5Nbb9ss66",
                          "playcount": 109003659,
                          "name": "Love You Anymore",
                          "popularity": 62,
                          "number": 3,
                          "duration": 182666,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1QELw50Dl95LusF6uOkDqk",
                          "playcount": 46905237,
                          "name": "La vie en rose (feat. Cécile McLorin Salvant)",
                          "popularity": 58,
                          "number": 4,
                          "duration": 229963,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Cécile McLorin Salvant",
                              "uri": "spotify:artist:6PkSULcbxFKkxdgrmPGAvn"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3CsGqy0ajJSqIDBe8fWJWt",
                          "playcount": 11712187,
                          "name": "My Funny Valentine",
                          "popularity": 40,
                          "number": 5,
                          "duration": 265654,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6QixSlgfhcMRoDDRQYYevd",
                          "playcount": 20009246,
                          "name": "Such a Night",
                          "popularity": 47,
                          "number": 6,
                          "duration": 197159,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:46zJ3FTTeK48LESza15wbQ",
                          "playcount": 32950237,
                          "name": "Forever Now",
                          "popularity": 57,
                          "number": 7,
                          "duration": 220248,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6f6jBn29HU6pbvy99NYXF3",
                          "playcount": 24744562,
                          "name": "Help Me Make It Through the Night (feat. Loren Allred)",
                          "popularity": 57,
                          "number": 8,
                          "duration": 222161,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Loren Allred",
                              "uri": "spotify:artist:0LyOADBjj28cbvJWTXUEGA"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4XawN6Nu5twzrJKRR6PV8C",
                          "playcount": 24988194,
                          "name": "Unforgettable",
                          "popularity": 55,
                          "number": 9,
                          "duration": 188028,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:76ZATrojG4UsYHhE6ulqNU",
                          "playcount": 10050417,
                          "name": "When You're Smiling",
                          "popularity": 49,
                          "number": 10,
                          "duration": 170691,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0S2qRDoO9KAfA1nJB6UsP0",
                          "playcount": 13366199,
                          "name": "Where or When",
                          "popularity": 40,
                          "number": 11,
                          "duration": 185601,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5cEap0vG6Dc6TJDN77cyF9",
                          "playcount": 12516360,
                          "name": "When You're Not Here",
                          "popularity": 42,
                          "number": 12,
                          "duration": 218261,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6AqCXpbWs2dTMWZqVULpG7",
                          "playcount": 13480818,
                          "name": "I Get a Kick out of You",
                          "popularity": 44,
                          "number": 13,
                          "duration": 177085,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 11,
                  "day": 16
                },
                {
                  "uri": "spotify:album:2OXZJLXxM8jrY3gBoVNfmz",
                  "name": "Nobody but Me (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02576629f3c4631eb55612a7c7"
                  },
                  "year": 2016,
                  "track_count": 13,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:67aeyMdt7cgb8l9zg53Pfm",
                          "playcount": 65296989,
                          "name": "I Believe in You",
                          "popularity": 56,
                          "number": 1,
                          "duration": 201813,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:13QWQQW83ZMO1k8EDucqgZ",
                          "playcount": 29472487,
                          "name": "My Kind of Girl",
                          "popularity": 42,
                          "number": 2,
                          "duration": 246440,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5G3UfEFiR4MUqkC8ETbzeR",
                          "playcount": 59520045,
                          "name": "Nobody but Me",
                          "popularity": 49,
                          "number": 3,
                          "duration": 179640,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4JEC2RIrXV1owJNVKanIAT",
                          "playcount": 30988662,
                          "name": "On an Evening in Roma (Sott'er Celo de Roma)",
                          "popularity": 58,
                          "number": 4,
                          "duration": 162520,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5QU2xDVhEpYb3qKZZuiYSG",
                          "playcount": 24073400,
                          "name": "Today Is Yesterday's Tomorrow",
                          "popularity": 41,
                          "number": 5,
                          "duration": 204533,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1eFOYKVociYWN0RwUgJFvY",
                          "playcount": 10833049,
                          "name": "The Very Thought of You",
                          "popularity": 36,
                          "number": 6,
                          "duration": 211613,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7bsEgxdswhGfqGspJp4vBd",
                          "playcount": 7377985,
                          "name": "I Wanna Be Around",
                          "popularity": 31,
                          "number": 7,
                          "duration": 222120,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7t3q4ghl6rphMXn0oOJhUr",
                          "playcount": 59447821,
                          "name": "Someday (feat. Meghan Trainor)",
                          "popularity": 62,
                          "number": 8,
                          "duration": 203453,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Meghan Trainor",
                              "uri": "spotify:artist:6JL8zeS1NmiOftqZTRgdTz"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7jVwYxRPooIkkOzqbfWkXB",
                          "playcount": 8477806,
                          "name": "My Baby Just Cares for Me",
                          "popularity": 35,
                          "number": 9,
                          "duration": 195680,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0vnCaQ0QtDnMYI5tVuhZiV",
                          "playcount": 4175495,
                          "name": "This Love of Mine",
                          "popularity": 32,
                          "number": 10,
                          "duration": 259959,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6T3jqFEIG2DoUygo3p45Yd",
                          "playcount": 59520045,
                          "name": "Nobody but Me - Alternate with Trumpet",
                          "popularity": 32,
                          "number": 11,
                          "duration": 181386,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2SUcmMZ9f97rMdTDnAvfiq",
                          "playcount": 5248964,
                          "name": "Take You Away",
                          "popularity": 35,
                          "number": 12,
                          "duration": 173213,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6gGboAhqHBqs5szVLobC41",
                          "playcount": 11008273,
                          "name": "God Only Knows",
                          "popularity": 36,
                          "number": 13,
                          "duration": 255400,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:4Yf5LJfqpjgl1a4TBiCi07",
                  "name": "To Be Loved",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02051ae642ad4a0c1329b41d99"
                  },
                  "year": 2013,
                  "track_count": 14,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:1B8RSIxmwcjad7XUJjeCK2",
                          "playcount": 44628946,
                          "name": "You Make Me Feel so Young",
                          "popularity": 54,
                          "number": 1,
                          "duration": 185693,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0mvkwaZMP2gAy2ApQLtZRv",
                          "playcount": 165962118,
                          "name": "It's a Beautiful Day",
                          "popularity": 70,
                          "number": 2,
                          "duration": 199266,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1JVf7tzhspVTRrRHxiJhD5",
                          "playcount": 53454398,
                          "name": "To Love Somebody",
                          "popularity": 59,
                          "number": 3,
                          "duration": 195360,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1i3QDZbA9jVI7Lg1DA122c",
                          "playcount": 20009302,
                          "name": "Who's Lovin' You",
                          "popularity": 44,
                          "number": 4,
                          "duration": 176040,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3wwXUFIYX044Tqiy3AFpLO",
                          "playcount": 68539135,
                          "name": "Something Stupid (feat. Reese Witherspoon)",
                          "popularity": 61,
                          "number": 5,
                          "duration": 177626,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Reese Witherspoon",
                              "uri": "spotify:artist:5V8q61RswIFvxhIfzYVew9"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0dJ9ijnTaxtBQA2tWsCnZB",
                          "playcount": 15899508,
                          "name": "Come Dance with Me",
                          "popularity": 45,
                          "number": 6,
                          "duration": 166173,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:04THq9ESnlipU969vuvSJx",
                          "playcount": 63810091,
                          "name": "Close Your Eyes",
                          "popularity": 58,
                          "number": 7,
                          "duration": 213106,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6l8tOyMl3Hige13zDSVtzF",
                          "playcount": 16256218,
                          "name": "After All (feat. Bryan Adams)",
                          "popularity": 46,
                          "number": 8,
                          "duration": 217480,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Bryan Adams",
                              "uri": "spotify:artist:3Z02hBLubJxuFJfhacLSDc"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7EBAGLLkK2qdtwZ0l1bOnV",
                          "playcount": 16688972,
                          "name": "Have I Told You Lately That I Love You (with Naturally 7)",
                          "popularity": 47,
                          "number": 9,
                          "duration": 206066,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Naturally 7",
                              "uri": "spotify:artist:769D3IwCDrdPospAd3Hlpi"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:62tLfE21g2SBxApaBvJ9r1",
                          "playcount": 15028374,
                          "name": "To Be Loved",
                          "popularity": 44,
                          "number": 10,
                          "duration": 221200,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5xEfTvJm1WvxaSEo4XSbEs",
                          "playcount": 25155063,
                          "name": "You've Got a Friend in Me",
                          "popularity": 52,
                          "number": 11,
                          "duration": 205693,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4DNfZAso4sNpBvLD7Xw1lL",
                          "playcount": 10155824,
                          "name": "Nevertheless (I'm in Love with You) (feat. The Puppini Sisters)",
                          "popularity": 36,
                          "number": 12,
                          "duration": 175293,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "The Puppini Sisters",
                              "uri": "spotify:artist:1svaANJTE5KrG16fTGDqOs"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1mqXsilUXcXQAMSx19zLEb",
                          "playcount": 10216669,
                          "name": "I Got It Easy",
                          "popularity": 36,
                          "number": 13,
                          "duration": 219640,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:02QwHNesGgFUpcRBosNdd5",
                          "playcount": 11108742,
                          "name": "Young at Heart",
                          "popularity": 39,
                          "number": 14,
                          "duration": 222720,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 4,
                  "day": 11
                },
                {
                  "uri": "spotify:album:53fJVD9LpBKEMqdAF7PW5K",
                  "name": "Christmas (Deluxe Special Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022ead7786631d8dd3b59be4f0"
                  },
                  "year": 2011,
                  "track_count": 21,
                  "discs": [
                    {
                      "number": 1,
                      "name": "",
                      "tracks": [
                        {
                          "uri": "spotify:track:1rv46mRwDqMEhOBZ7vODg3",
                          "playcount": 806727841,
                          "name": "It's Beginning to Look a Lot like Christmas",
                          "popularity": 48,
                          "number": 1,
                          "duration": 207199,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:7Ivelh8N9J25Yj9KKxYj6w",
                          "playcount": 252103167,
                          "name": "Santa Claus Is Coming to Town",
                          "popularity": 35,
                          "number": 2,
                          "duration": 171806,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3nfxV3jfRcRWISwek49LTg",
                          "playcount": 185792518,
                          "name": "Jingle Bells (feat. The Puppini Sisters)",
                          "popularity": 33,
                          "number": 3,
                          "duration": 161804,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "The Puppini Sisters",
                              "uri": "spotify:artist:1svaANJTE5KrG16fTGDqOs"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:114461rbzcgTriw8n4FOPW",
                          "playcount": 167871409,
                          "name": "White Christmas (with Shania Twain)",
                          "popularity": 31,
                          "number": 4,
                          "duration": 218465,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Shania Twain",
                              "uri": "spotify:artist:5e4Dhzv426EvQe3aDb64jL"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5S8rkSt8wio92kjIOjbJ0U",
                          "playcount": 166461897,
                          "name": "All I Want for Christmas Is You",
                          "popularity": 32,
                          "number": 5,
                          "duration": 174085,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:67mgz7S5y7hnCE63YBjfO6",
                          "playcount": 475876500,
                          "name": "Holly Jolly Christmas",
                          "popularity": 44,
                          "number": 6,
                          "duration": 121526,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2v3sPOCHJd76JDJKf1I2nb",
                          "playcount": 112760472,
                          "name": "Santa Baby",
                          "popularity": 27,
                          "number": 7,
                          "duration": 232949,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4QMEcIbG9GtSrkbvk7Pswd",
                          "playcount": 142898770,
                          "name": "Have Yourself a Merry Little Christmas",
                          "popularity": 31,
                          "number": 8,
                          "duration": 232106,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4xaivtewmKqsr6dLLTSGCY",
                          "playcount": 145306957,
                          "name": "Christmas (Baby Please Come Home)",
                          "popularity": 31,
                          "number": 9,
                          "duration": 189794,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3Iev01vjk4AQP2AU36wNvU",
                          "playcount": 103141388,
                          "name": "Silent Night",
                          "popularity": 27,
                          "number": 10,
                          "duration": 230059,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1I5FKh97dvlA9mMdJ2BZND",
                          "playcount": 93492085,
                          "name": "Blue Christmas",
                          "popularity": 25,
                          "number": 11,
                          "duration": 223927,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1XW3TCZp4a6koiyEpqBmPG",
                          "playcount": 141134037,
                          "name": "Cold December Night",
                          "popularity": 29,
                          "number": 12,
                          "duration": 200704,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0GyXfYRuvG9nt71zF99SER",
                          "playcount": 168844196,
                          "name": "I'll Be Home for Christmas",
                          "popularity": 38,
                          "number": 13,
                          "duration": 267608,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:1i7JjMvpzUCFR6GiqSaenp",
                          "playcount": 72311165,
                          "name": "Ave Maria",
                          "popularity": 29,
                          "number": 14,
                          "duration": 243031,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:5kidjcHqKZzTs6bD2mM7ZY",
                          "playcount": 82230153,
                          "name": "Mis Deseos / Feliz Navidad (with Thalia)",
                          "popularity": 27,
                          "number": 15,
                          "duration": 264878,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Thalia",
                              "uri": "spotify:artist:23wEWD21D4TPYiJugoXmYb"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:2rDeOU9Y1xRvvIhaqHEoxW",
                          "playcount": 68065136,
                          "name": "The Christmas Song (Chestnuts Roasting on an Open Fire)",
                          "popularity": 27,
                          "number": 16,
                          "duration": 260095,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4hNouIhwvI8FKeEMeWEvbg",
                          "playcount": 79854686,
                          "name": "Winter Wonderland",
                          "popularity": 30,
                          "number": 17,
                          "duration": 150880,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:6RJJUEOfUyMhcxtsfLNRpH",
                          "playcount": 70673273,
                          "name": "Frosty the Snowman (feat. The Puppini Sisters)",
                          "popularity": 30,
                          "number": 18,
                          "duration": 162482,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "The Puppini Sisters",
                              "uri": "spotify:artist:1svaANJTE5KrG16fTGDqOs"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:0uqFTuDLbIyCZG9yNYsqcG",
                          "playcount": 54874356,
                          "name": "Silver Bells (feat. Naturally 7)",
                          "popularity": 25,
                          "number": 19,
                          "duration": 189107,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            },
                            {
                              "name": "Naturally 7",
                              "uri": "spotify:artist:769D3IwCDrdPospAd3Hlpi"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:4buHBCFNm5WvttEDevaj9C",
                          "playcount": 37945382,
                          "name": "White Christmas",
                          "popularity": 25,
                          "number": 20,
                          "duration": 204156,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        },
                        {
                          "uri": "spotify:track:3iYCZZNVNLlm4ed4OlU76w",
                          "playcount": 17389,
                          "name": "Michael's Christmas Greeting",
                          "popularity": 0,
                          "number": 21,
                          "duration": 8191,
                          "explicit": false,
                          "playable": true,
                          "artists": [
                            {
                              "name": "Michael Bublé",
                              "uri": "spotify:artist:1GxkXlMwML1oSg5eLPiAz3"
                            }
                          ]
                        }
                      ]
                    }
                  ],
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:2dNQDOzCubrvIWxtaYO6cU",
                  "name": "Crazy Love (Hollywood Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ecf8d272ecbb4d59e1ec2929"
                  },
                  "year": 2009,
                  "track_count": 22,
                  "month": 10,
                  "day": 9
                },
                {
                  "uri": "spotify:album:3MXDonOIzrIrCh0HvlACyj",
                  "name": "Crazy Love",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f0cc194252888c6658c706ab"
                  },
                  "year": 2009,
                  "track_count": 13,
                  "month": 10,
                  "day": 6
                },
                {
                  "uri": "spotify:album:52I28CFjJlE3locLAcHDJt",
                  "name": "Michael Bublé Meets Madison Square Garden",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a051cb3cfd065a9eb0d7bc2b"
                  },
                  "year": 2009,
                  "track_count": 10,
                  "month": 6,
                  "day": 12
                },
                {
                  "uri": "spotify:album:5dXU3rx7fHkNJ1kTzvRQb3",
                  "name": "Call Me Irresponsible (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d72cafc06acc89d30bf6093a"
                  },
                  "year": 2007,
                  "track_count": 14,
                  "month": 5,
                  "day": 1
                },
                {
                  "uri": "spotify:album:3h4pyWRJIB9ZyRKXChbX22",
                  "name": "Call Me Irresponsible",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022ceedc8c879a1f6784fbeef5"
                  },
                  "year": 2007,
                  "track_count": 13,
                  "month": 4,
                  "day": 30
                },
                {
                  "uri": "spotify:album:0KK6xo3C9WXTdkuj6n3ZUB",
                  "name": "Caught in the Act",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c1478d49d296237635754d0a"
                  },
                  "year": 2005,
                  "track_count": 8,
                  "month": 11,
                  "day": 15
                },
                {
                  "uri": "spotify:album:1f9vWKabhNPNQnHLleExSh",
                  "name": "It's Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c6ad2b2b62b581a23a7c1759"
                  },
                  "year": 2005,
                  "track_count": 15,
                  "month": 2,
                  "day": 8
                },
                {
                  "uri": "spotify:album:0UhvDeKmtgegXeELEVgGRh",
                  "name": "Come Fly with Me",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0211ee8f400df1c708db8fa471"
                  },
                  "year": 2004,
                  "track_count": 8,
                  "month": 3,
                  "day": 30
                },
                {
                  "uri": "spotify:album:1vQKqcvalwlGF8mAhgbADB",
                  "name": "Totally Bublé",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b5177c2541bb3d25deb33af1"
                  },
                  "year": 2003,
                  "track_count": 7,
                  "month": 9,
                  "day": 9
                },
                {
                  "uri": "spotify:album:3rpSksJSFdNFqk5vne8at2",
                  "name": "Michael Bublé",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b732a522a686bb304a5d3fdf"
                  },
                  "year": 2003,
                  "track_count": 13
                }
              ],
              "total_count": 18
            },
            "singles": {
              "releases": [
                {
                  "uri": "spotify:album:15ztGKbv3PTtRmLHpjIP5L",
                  "name": "Holly Jolly Christmas (Sped Up)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f69e8d67f79bc2eeb743230d"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:2V64XxgnT3QvQdSbUuWiEi",
                  "name": "It's Beginning to Look a Lot like Christmas (Sped Up)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020520239aec66dd96d87a257c"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:3p2w6yULdjvz0X9ywEpZao",
                  "name": "Crazy (with Jon Batiste & Stay Human) [Live]",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02719049049d23a1b8132e46da"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 8,
                  "day": 26
                },
                {
                  "uri": "spotify:album:5fo1WaFXZlz0BKykcA3JCf",
                  "name": "Michael Bublé's Christmas Party",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02368fbfee5dec0d09bff0b89f"
                  },
                  "year": 2022,
                  "track_count": 6,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:4MWmbUxzSeGWSJOyfXhZnZ",
                  "name": "Michael Bublé's Cozy Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026e7286ad197115902a0d14c0"
                  },
                  "year": 2022,
                  "track_count": 6,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:7CSoun3i75YZCgqo7M7u4X",
                  "name": "Michael Bublé's Family Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0215990a9f7cf82f669e4b237a"
                  },
                  "year": 2022,
                  "track_count": 6,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:4WNLnLibAOI8cgv88DzvGN",
                  "name": "Michael Bublé's Romantic Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0241aabd6629821aa9649f6c2c"
                  },
                  "year": 2022,
                  "track_count": 6,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:4fLA8CGh1Ip9VsgndFIj7K",
                  "name": "Drivers License (feat. BBC Concert Orchestra) [Live at the BBC]",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e1b11846379c89a2b12df41d"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 7,
                  "day": 15
                },
                {
                  "uri": "spotify:album:3qQb4brcSYgXOPVALspjL3",
                  "name": "Sway (Sped Up Version)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02faf8a479a91bfebf193eae05"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 5,
                  "day": 13
                },
                {
                  "uri": "spotify:album:61WnLVI6BAd9DcsVOVXaF5",
                  "name": "A Nightingale Sang in Berkeley Square",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e8ea776c20025a826554a6a7"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 3,
                  "day": 18
                },
                {
                  "uri": "spotify:album:0pC2nDSKjfCdEth7murfOu",
                  "name": "Higher",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02dfe97c5078b939f5596bb246"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:3OQvJiraXKN7zo82ZOy2YT",
                  "name": "My Valentine",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bd75e169cbe4486bb0c34c2d"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 2,
                  "day": 11
                },
                {
                  "uri": "spotify:album:40Ud7EvNeSxYcpNibD0Qhu",
                  "name": "I'll Never Not Love You",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a162fa0fd3c7165b00be5a9e"
                  },
                  "year": 2022,
                  "track_count": 1,
                  "month": 1,
                  "day": 28
                },
                {
                  "uri": "spotify:album:17Qr6J860gp3HrESuJ5D2M",
                  "name": "Let It Snow! (10th Anniversary)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ca1cbdfd5e824b2a4bf4a43e"
                  },
                  "year": 2021,
                  "track_count": 1,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:5HpFEGkt4ANzgWWIFBAFjL",
                  "name": "Elita (feat. Michael Bublé & Sebastián Yatra)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021767c8db34c9abef7b10f8c7"
                  },
                  "year": 2020,
                  "track_count": 1,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:6zqf7sIi51QWskNvkEYaA7",
                  "name": "Gotta Be Patient",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020708398c2f2cdfce5b43b6d5"
                  },
                  "year": 2020,
                  "track_count": 1,
                  "month": 5,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4N7icvLRajxakOXGnRjUMR",
                  "name": "White Christmas / 'Twas the Night Before Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02edc52916392304e99f3afb1c"
                  },
                  "year": 2019,
                  "track_count": 2,
                  "month": 12,
                  "day": 6
                },
                {
                  "uri": "spotify:album:7ERFXgfx9To2P8DH9a7pW8",
                  "name": "White Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024189772d572e44903e77574d"
                  },
                  "year": 2019,
                  "track_count": 1,
                  "month": 11,
                  "day": 1
                },
                {
                  "uri": "spotify:album:45TIKE2URFPyJlo6UA2kMl",
                  "name": "Spotify Singles",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02772dfd7e2c543bb6be0322d1"
                  },
                  "year": 2019,
                  "track_count": 2,
                  "month": 2,
                  "day": 13
                },
                {
                  "uri": "spotify:album:16wzZ5nFiLWoUeltgXQAlL",
                  "name": "Love You Anymore (Cook Classics Remix)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0275a95a608073555a8d92fb7a"
                  },
                  "year": 2019,
                  "track_count": 2,
                  "month": 1,
                  "day": 25
                },
                {
                  "uri": "spotify:album:58X6ic2C9hCW8MLANoWEH6",
                  "name": "The More You Give (The More You'll Have)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02747548a9525652fe3f5bec5b"
                  },
                  "year": 2015,
                  "track_count": 1,
                  "month": 11,
                  "day": 30
                },
                {
                  "uri": "spotify:album:0odXxGyN1031B8sZGDrvgr",
                  "name": "After All (feat. Bryan Adams)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02415cb10e54121a6897288547"
                  },
                  "year": 2013,
                  "track_count": 1,
                  "month": 9,
                  "day": 27
                },
                {
                  "uri": "spotify:album:2hKWa045n0QS1Wo2nDfVOR",
                  "name": "It's a Beautiful Day",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024366af3b9624690e213fb1b1"
                  },
                  "year": 2013,
                  "track_count": 1,
                  "month": 2,
                  "day": 25
                },
                {
                  "uri": "spotify:album:46MKtaehCqAKT97GN4m5bD",
                  "name": "White Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02479538ab80314545255571f8"
                  },
                  "year": 2012,
                  "track_count": 1,
                  "month": 12,
                  "day": 12
                },
                {
                  "uri": "spotify:album:3uTPJvoA6ck9juIaDCytSH",
                  "name": "White Christmas (with Shy'm)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ab034ff81512ccbf92232219"
                  },
                  "year": 2011,
                  "track_count": 1,
                  "month": 10,
                  "day": 24
                },
                {
                  "uri": "spotify:album:5FIihZej1n9abjAZAYhVSl",
                  "name": "Haven't Met You Yet",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020c3ebff9d962153e29dcc6c8"
                  },
                  "year": 2010,
                  "track_count": 5,
                  "month": 8,
                  "day": 16
                },
                {
                  "uri": "spotify:album:6SFiqF4iSwb920rjgFWAI3",
                  "name": "Special Delivery",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d5caf58fc1b8b048922c7f4d"
                  },
                  "year": 2010,
                  "track_count": 6,
                  "month": 2,
                  "day": 5
                },
                {
                  "uri": "spotify:album:2mLilUD8WZQwDhwmnMmdUF",
                  "name": "Baby (You've Got What It Takes)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029120e4a9564b0e07f4e9c5d2"
                  },
                  "year": 2009,
                  "track_count": 10,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:7f88ikdmSNH3G56AEs8rev",
                  "name": "It Had Better Be Tonight - The Remixes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b52152ec59f590c800ffa5f6"
                  },
                  "year": 2007,
                  "track_count": 7,
                  "month": 4,
                  "day": 30
                },
                {
                  "uri": "spotify:album:1Ytcv0Z8lcWucefqljdY2K",
                  "name": "Save the Last Dance for Me EP",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020d5491346ddc7ae4a0f90319"
                  },
                  "year": 2005,
                  "track_count": 4,
                  "month": 2,
                  "day": 8
                },
                {
                  "uri": "spotify:album:4OEmDYaSHrnrJc16LKo1C9",
                  "name": "Spider-Man Theme / Sway Remixes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022bda28ca7a260d0722daa154"
                  },
                  "year": 2003,
                  "track_count": 6,
                  "month": 2,
                  "day": 11
                }
              ],
              "total_count": 31
            },
            "appears_on": {
              "releases": [
                {
                  "uri": "spotify:album:6xUodRTpBiWXfQwPVZ5hIN",
                  "name": "Wallflower",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e92ff8f74f4ee9efb4587795"
                  },
                  "year": 2014,
                  "track_count": 12,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:3BYCjGZjrTkilIY7U25fNt",
                  "name": "If I Can Dream: Elvis Presley with the Royal Philharmonic Orchestra",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027adaca103b3a58a478bac9f4"
                  },
                  "year": 2015,
                  "track_count": 14,
                  "month": 10,
                  "day": 30
                },
                {
                  "uri": "spotify:album:5HDevGeDLIZMhZKvRZLSkI",
                  "name": "Habítame Siempre (Bonus Tracks Version)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025f328d82af83c0ccb50c504e"
                  },
                  "year": 2012,
                  "track_count": 15,
                  "month": 11,
                  "day": 19
                },
                {
                  "uri": "spotify:album:5ktAYFq8wJ1hcWHaxl6AQf",
                  "name": "Duets II",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02796c0744b04db30174704d24"
                  },
                  "year": 2011,
                  "track_count": 17,
                  "month": 9,
                  "day": 20
                },
                {
                  "uri": "spotify:album:4yY8NL5zw83iBAvqVuUAL4",
                  "name": "Swings Both Ways (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022e60ff0f37e6504fcc503981"
                  },
                  "year": 2013,
                  "track_count": 16,
                  "month": 11,
                  "day": 18
                },
                {
                  "uri": "spotify:album:3cdclv2AHxC4DTjlIujNeC",
                  "name": "Duets",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e7afa9599b308a962f843ed5"
                  },
                  "year": 2013,
                  "track_count": 14,
                  "month": 4,
                  "day": 5
                },
                {
                  "uri": "spotify:album:0vpZmvUH5x2ByXTYtXB4mG",
                  "name": "Partners (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0208a0d253d27a5bdb39626ea2"
                  },
                  "year": 2014,
                  "track_count": 17
                },
                {
                  "uri": "spotify:album:5ymzYusupvHy8ViKep3vc6",
                  "name": "Wallflower (The Complete Sessions)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d554b63a3fd95a9db9e08030"
                  },
                  "year": 2015,
                  "track_count": 20,
                  "month": 9,
                  "day": 18
                },
                {
                  "uri": "spotify:album:7zXao5a6CPcsA8mbmdKpVs",
                  "name": "You're The Inspiration: The Music Of David Foster And Friends (Int'l DMD)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02edbed03455d39ebba4abff39"
                  },
                  "year": 2008,
                  "track_count": 12,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:0RPgKTqFhjUD8KEf9vR7jX",
                  "name": "Duets An American Classic",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c2c103caf406c34746736307"
                  },
                  "year": 2006,
                  "track_count": 19,
                  "month": 9,
                  "day": 26
                },
                {
                  "uri": "spotify:album:1WZPexv6jHN4AmvJ52WZHh",
                  "name": "Duets: Re-Working The Catalogue",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0289fd66d83d6172b9fb923d77"
                  },
                  "year": 2015,
                  "track_count": 16,
                  "month": 3,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3CBMpoI2vZlKXs3wgnNWGn",
                  "name": "20 The Greatest Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02918853834b4d976a41829dff"
                  },
                  "year": 2013,
                  "track_count": 38,
                  "month": 10,
                  "day": 31
                },
                {
                  "uri": "spotify:album:1xKo2KKccXTTmV8cqKFRBf",
                  "name": "Caméléon (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0238e5d937f65fd1cb15d686ee"
                  },
                  "year": 2012,
                  "track_count": 15,
                  "month": 6,
                  "day": 25
                },
                {
                  "uri": "spotify:album:1gnG5goU9r0ULPX8X4MDHZ",
                  "name": "Love (International Version)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a960e9048bc2a646f5f6ca09"
                  },
                  "year": 2009,
                  "track_count": 13,
                  "month": 11,
                  "day": 24
                },
                {
                  "uri": "spotify:album:7fPGIJOxWbsae3wWYnydNj",
                  "name": "Lean on Me - ArtistsCAN",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029462d06649e717b9b1925185"
                  },
                  "year": 2020,
                  "track_count": 1,
                  "month": 4,
                  "day": 27
                },
                {
                  "uri": "spotify:album:7cKOW2P65kWE5xAxgpchqU",
                  "name": "Music From And Inspired By The Motion Picture Tyler Perry's Why Did I Get Married?",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ae550e4690e5201fb0ff309f"
                  },
                  "year": 2007,
                  "track_count": 14,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:6TDpQggB88X230Srv1fkSq",
                  "name": "100 Greatest Christmas Songs Ever",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bb653f725d8a3d0c58983111"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:1IgRzhudBVPtta2jDVa1en",
                  "name": "Partners",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b5f9f95f92dc403fe7883180"
                  },
                  "year": 2014,
                  "track_count": 12,
                  "month": 9,
                  "day": 10
                },
                {
                  "uri": "spotify:album:52bodfQDr1a2Nmk6C51i3s",
                  "name": "Habítame Siempre",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02632eda787ea79785a183e4e2"
                  },
                  "year": 2013,
                  "track_count": 13,
                  "month": 2,
                  "day": 8
                },
                {
                  "uri": "spotify:album:0E3TKLXbAvLysz9fcaWSbR",
                  "name": "PER SEMPRE",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0251d975a6d54b596d742c7d86"
                  },
                  "year": 2022,
                  "track_count": 15,
                  "month": 8,
                  "day": 19
                },
                {
                  "uri": "spotify:album:7DwZcZjbCbluvYCOrIIKi6",
                  "name": "To Love Again",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0259cdf8f5b96bd540166f568b"
                  },
                  "year": 2005,
                  "track_count": 13,
                  "month": 10,
                  "day": 18
                },
                {
                  "uri": "spotify:album:06PwBHYewAgZCoOpJpxPjv",
                  "name": "PER SEMPRE (EDIZIONE DA CAPO)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0299c0d0f4d2df009a53e6ec19"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 5,
                  "day": 5
                },
                {
                  "uri": "spotify:album:3Qxl28cwKrpLzYhHbvejqV",
                  "name": "Making Memories",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020683c035fa8031090e634472"
                  },
                  "year": 2021,
                  "track_count": 13,
                  "month": 8,
                  "day": 13
                },
                {
                  "uri": "spotify:album:0EGX5qfw6VEPOMoCUFJFHl",
                  "name": "Holiday Wishes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b942e9ff43d692b700328ecc"
                  },
                  "year": 2014,
                  "track_count": 12,
                  "month": 10,
                  "day": 10
                },
                {
                  "uri": "spotify:album:535jupBs3E4mgR18cyI52P",
                  "name": "Gracias a la vida",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02affffca57db32738ca919cdf"
                  },
                  "year": 2010,
                  "track_count": 1,
                  "month": 5,
                  "day": 4
                },
                {
                  "uri": "spotify:album:4QQakC5DZhjYWtOAFlsxfR",
                  "name": "Road Trip",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e952c561d3289c07f2a533bc"
                  },
                  "year": 2022,
                  "track_count": 75,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:0Uo3Vv7z4jnFoOkQgrM0k5",
                  "name": "The Best Of Nelly Furtado (Deluxe Version)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ec7966560356a3438b54a63d"
                  },
                  "year": 2010,
                  "track_count": 26,
                  "month": 11,
                  "day": 16
                },
                {
                  "uri": "spotify:album:1VrravzsxruIeQf1cuE6zB",
                  "name": "Classic Oldies",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0212dafc73ddff160e2a9e8b14"
                  },
                  "year": 2022,
                  "track_count": 75,
                  "month": 8,
                  "day": 19
                },
                {
                  "uri": "spotify:album:4PEuqYJixNSCMotzbfmWNb",
                  "name": "60 Years: The Artistry of Tony Bennett",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02838907b56e8015b7bf9ab2cf"
                  },
                  "year": 2013,
                  "track_count": 131,
                  "month": 10,
                  "day": 4
                },
                {
                  "uri": "spotify:album:3YBHGYTcnNwa0TcwpMUKk9",
                  "name": "Silvester Party Hits 2022 / 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020543421c2ebffe8995907e7c"
                  },
                  "year": 2022,
                  "track_count": 78,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:3hJ53zBqiZbocDKlB3NXxt",
                  "name": "Music Played By Humans (Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b8f1119e184eca3eb6a060f6"
                  },
                  "year": 2020,
                  "track_count": 19,
                  "month": 11,
                  "day": 27
                },
                {
                  "uri": "spotify:album:2lWmhZbjmXYQRvTO7gkMTR",
                  "name": "Habítame Siempre Edición Especial",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025c008b6c709b8a7093d521aa"
                  },
                  "year": 2013,
                  "track_count": 17,
                  "month": 8,
                  "day": 27
                },
                {
                  "uri": "spotify:album:4Jk9oJP4aUo6X2XSwTgnBh",
                  "name": "Cheers, It's Christmas (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ecdf03038185f94a5fb1bcf3"
                  },
                  "year": 2012,
                  "track_count": 17,
                  "month": 10,
                  "day": 2
                },
                {
                  "uri": "spotify:album:63cg3nGFJXmCeh9odxBpgI",
                  "name": "The Ultimate Collection (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02431545a19cea56bafe915ef8"
                  },
                  "year": 2017,
                  "track_count": 41,
                  "month": 9,
                  "day": 29
                },
                {
                  "uri": "spotify:album:4hdsbyN30keENMDd75bA9V",
                  "name": "Cinco de Mayo Party 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02430d80dbf38fad28e12feed5"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 4,
                  "day": 28
                },
                {
                  "uri": "spotify:album:3fznq3g09X53pgwKg57KbJ",
                  "name": "Tony Bennett Celebrates 90",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0237f960b83eaa1046751aeaa9"
                  },
                  "year": 2016,
                  "track_count": 18,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:7mcyR9Z35tlZoa0nC7PnuH",
                  "name": "Merry Christmas, Baby",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023bc6032a006f5d86f5122a46"
                  },
                  "year": 2012,
                  "track_count": 13,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:7AufJYxxSwzzTvLpTtGQl1",
                  "name": "To Love Again - Holiday Gift Pack",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026a1ed159e100ad7577c58cc7"
                  },
                  "year": 2002,
                  "track_count": 19
                },
                {
                  "uri": "spotify:album:1XLRWSpi9oiiVl3dkbAm6i",
                  "name": "Oster Frühstück",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029062c50385e235654c385aff"
                  },
                  "year": 2023,
                  "track_count": 42,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:6Q8FViKVe8VHIFaZLw7NKB",
                  "name": "Oster Brunch",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027673a3fb6f023c714a0f1111"
                  },
                  "year": 2023,
                  "track_count": 34,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:46qrICPWlwqpIPirMfAd7K",
                  "name": "Elvis symphonique",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c9349ca8d7bab313a5cda9cd"
                  },
                  "year": 2017,
                  "track_count": 34,
                  "month": 8,
                  "day": 4
                },
                {
                  "uri": "spotify:album:6ZImJQDuW7wnfP1NGJQptv",
                  "name": "The Best of The King's Singers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ac02b09ccf884345678f0fdd"
                  },
                  "year": 2012,
                  "track_count": 40,
                  "month": 9,
                  "day": 24
                },
                {
                  "uri": "spotify:album:7BKMLhbGps9SDjUh8FEv8o",
                  "name": "Cheers, It's Christmas (Super Deluxe)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02792023c3c481d18f6a320082"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:4oPF9ffM4IQPZffFRdyXPT",
                  "name": "Voices Of The Valley: Home",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a34e7865f293ced058472735"
                  },
                  "year": 2008,
                  "track_count": 13,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:70Qb6bUqq4qI46PrVtG4gB",
                  "name": "The Classics (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b50fbd4f25570b7979800b56"
                  },
                  "year": 2013,
                  "track_count": 30,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:2NzXEqvkivs9ZhiBiQb7nF",
                  "name": "We All Love Ella: Celebrating The First Lady Of Song",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0249105e8042c3f31d68d1c060"
                  },
                  "year": 2007,
                  "track_count": 16,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:6Wnovhi56DG9BpxufQLSER",
                  "name": "Classic Songs, My Way",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028d4a0bbff750f0db27eebb95"
                  },
                  "year": 2007,
                  "track_count": 13,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:5x6qjEVVjQo4tLmQDYNEwN",
                  "name": "In the Stars - Fresh Picks",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b2050ddbd756d4940d33762c"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:0UKCFUDo5hCdAB4b6tPqQe",
                  "name": "A Holly Dolly Christmas (Ultimate Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020b69ff049d3bf203a5fdc2bf"
                  },
                  "year": 2020,
                  "track_count": 20,
                  "month": 10,
                  "day": 2
                },
                {
                  "uri": "spotify:album:3mCaKiRTEdj52pxEbvh42A",
                  "name": "Gift Wrapped: Regifted",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b1204dff580c4d6771b441a5"
                  },
                  "year": 2010,
                  "track_count": 19,
                  "month": 11,
                  "day": 2
                },
                {
                  "uri": "spotify:album:3Xt0c0L8gkx8pKWqaHe0wo",
                  "name": "Hit Man David Foster & Friends (Amazon Excl.)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022c236b4712d46bc878dc6eaa"
                  },
                  "year": 2008,
                  "track_count": 10,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:6OfabbeDEMH1kCsXsZkH4x",
                  "name": "Down with Love (Music from and Inspired by the Motion Picture)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0244f21530511107c42cd7e87d"
                  },
                  "year": 2003,
                  "track_count": 12,
                  "month": 5,
                  "day": 13
                },
                {
                  "uri": "spotify:album:4KxCpuvcixkHoe5OM8nYl9",
                  "name": "Soirée Raclette",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020c24c2c49552186d476ae6e5"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:2gFe0K8uKPllO0uxVc8dwD",
                  "name": "Bing Sings The Irving Berlin Songbook",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020b9112ce631935edc5a3ee83"
                  },
                  "year": 2014,
                  "track_count": 22,
                  "month": 11,
                  "day": 24
                },
                {
                  "uri": "spotify:album:04KowFQhH1EvlLQZHh74q5",
                  "name": "Coffee Moment",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022148168cccd34d801beb5f79"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:6IRBJEl3iDCSTwATVsQfzX",
                  "name": "Barenaked For The Holidays",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b54912c6f036a0349745b15c"
                  },
                  "year": 2014,
                  "track_count": 20,
                  "month": 11,
                  "day": 24
                },
                {
                  "uri": "spotify:album:6rcW0B3FqEjG6h0V3sSZ7O",
                  "name": "30 Rock",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027ef5d624d6987e469d355b79"
                  },
                  "year": 2010,
                  "track_count": 31,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2izvuxJZ0wqpVH5pt6C64e",
                  "name": "Hit Anni 2000",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027ce9a69885f3fbfe183d0475"
                  },
                  "year": 2023,
                  "track_count": 44,
                  "month": 4,
                  "day": 28
                },
                {
                  "uri": "spotify:album:3jVwNk23VJ3IkJhAE02wVJ",
                  "name": "Sped Up Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021fabaf6931de28cfc475aa7d"
                  },
                  "year": 2023,
                  "track_count": 23,
                  "month": 1,
                  "day": 27
                },
                {
                  "uri": "spotify:album:33oyq4yn38yZKav02Zcg4w",
                  "name": "Clean Pop Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028820187f7c615b9a5da4e44f"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 1,
                  "day": 13
                },
                {
                  "uri": "spotify:album:2fuuiX6UvAI4G9nQqwryfZ",
                  "name": "Happy Holidays",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029fc4fff817ad8662a48c5ab3"
                  },
                  "year": 2021,
                  "track_count": 31,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:1TTRKFwM6ccmHdEpt37L7Z",
                  "name": "A Holly Dolly Christmas (Bonus Version)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0220aa03fb9202148bd6d5a792"
                  },
                  "year": 2020,
                  "track_count": 13,
                  "month": 12,
                  "day": 4
                },
                {
                  "uri": "spotify:album:6aG9G9gMURD17Wty41hPwv",
                  "name": "I Only Have Eyes for You",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0278867a8cb5ba709f8660956b"
                  },
                  "year": 2014,
                  "track_count": 11,
                  "month": 1,
                  "day": 9
                },
                {
                  "uri": "spotify:album:54V9FJEPswiDcvShYOmpwR",
                  "name": "The Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02584f109fae8960b812380f19"
                  },
                  "year": 2013,
                  "track_count": 20,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:3OVZT2d3edPeXz2CzCa2ng",
                  "name": "Romantique",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023f1114b3ccbb1b5277eb1814"
                  },
                  "year": 2013,
                  "track_count": 16,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4Dm5dkOZX4EX37AMCXI8oP",
                  "name": "Ballads - 100 Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c0655e1fd2e69187ae0c5b76"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:0jKDUk3tLqQ7YpJie0SjNb",
                  "name": "Top Hits México",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0247bb6c127c2adeb5eb430474"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:5D1mNvYByw4RlyigPLgUyZ",
                  "name": "Miss California - 00's Best",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027558e5a957706d9f464d7688"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:0K6SFTHvDLhzqPdGMha68Z",
                  "name": "Hits 2000",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a520594c0b0c671b41d1dd4c"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:1GpEvp66aXlponro87dWOV",
                  "name": "Songs to Sing In the Shower 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0215680e442be9a2427dc851dd"
                  },
                  "year": 2022,
                  "track_count": 31,
                  "month": 7,
                  "day": 15
                },
                {
                  "uri": "spotify:album:2aNwDScTeNVRQEiqa42tVs",
                  "name": "We All Love Ella: Celebrating The First Lady Of Song",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bcf26bd6c9a3a337965f31c2"
                  },
                  "year": 2007,
                  "track_count": 16,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2uT7uZVRm15Lcrjq7tX47g",
                  "name": "Shape of You - 10's Best",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024133915fb56d053d912a18d1"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 5,
                  "day": 26
                },
                {
                  "uri": "spotify:album:2CP8yfWqLfkaQQHbLYnhyh",
                  "name": "Summer Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0287e8fa11ad2e3ac69345fc7f"
                  },
                  "year": 2023,
                  "track_count": 75,
                  "month": 5,
                  "day": 6
                },
                {
                  "uri": "spotify:album:5ulmbnYgvjo9gCAHzbDQr5",
                  "name": "50 migliori canzoni da matrimonio",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e9bab2eceb54a850cefe12b0"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 4,
                  "day": 14
                },
                {
                  "uri": "spotify:album:7AykMHB8LZ9j3AjwmR8CG1",
                  "name": "Timeless Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0224a1826c805adc23ef6573fd"
                  },
                  "year": 2022,
                  "track_count": 65,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:7g6V1Ue3CIrJ6ehtNz8v9b",
                  "name": "Barenaked For The Holidays (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02af6d168610ee3214605fe55f"
                  },
                  "year": 2022,
                  "track_count": 29,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:1U0WexsXDjontsnGX3t7us",
                  "name": "Twenty Tens Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02dd6423f60ec4d9b9838493f6"
                  },
                  "year": 2023,
                  "track_count": 85,
                  "month": 5,
                  "day": 26
                },
                {
                  "uri": "spotify:album:6rUwmFp8onSZFMjvAhqbvm",
                  "name": "Viral Hits 2023 Trending TikTok Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f4d5939e449c4058140b2ab9"
                  },
                  "year": 2023,
                  "track_count": 49,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:0eTUk9ET7j8mm9lSV9E9DT",
                  "name": "New Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02934599351403b5cd47eec8c6"
                  },
                  "year": 2023,
                  "track_count": 44,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:1vu7E88Du7iTQHf9bW4bSn",
                  "name": "Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a81f5cbe33f2a49b72cde1b9"
                  },
                  "year": 2018,
                  "track_count": 55,
                  "month": 11,
                  "day": 30
                },
                {
                  "uri": "spotify:album:1WruQgTux8nkDL5eKA3N5t",
                  "name": "VocalPlay",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0284dc247671699149bfd0be47"
                  },
                  "year": 2010,
                  "track_count": 16,
                  "month": 4,
                  "day": 30
                },
                {
                  "uri": "spotify:album:3Kv2tDKNg4eWAaQDirVjxX",
                  "name": "Hey Mickey - Viral & Trending",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02871655d073e27d3fdebb6950"
                  },
                  "year": 2023,
                  "track_count": 70,
                  "month": 4,
                  "day": 27
                },
                {
                  "uri": "spotify:album:2qscmKMpqVguBNQ08PFKUc",
                  "name": "Daily Mix of Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e960ebdd55eff9ee95dc85fb"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:2ZzS0QL27QexnwSJvj1CSc",
                  "name": "An Elegant Affair",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027cde38bab5e0291e797d3910"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 6,
                  "day": 24
                },
                {
                  "uri": "spotify:album:4PQvoTkfMGWAGNSXadP7AX",
                  "name": "The Ultimate Christmas Collection",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a2d6a8f8f07f96c1e8ddd867"
                  },
                  "year": 2020,
                  "track_count": 22,
                  "month": 11,
                  "day": 20
                },
                {
                  "uri": "spotify:album:3j47GWo6Gu4KY2FiOskzEw",
                  "name": "Summer Cool Jazz",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024524cf0a3c044cfa368ce2e3"
                  },
                  "year": 2018,
                  "track_count": 32,
                  "month": 7,
                  "day": 10
                },
                {
                  "uri": "spotify:album:11E8YDVhtljf7D9jVUL62i",
                  "name": "Merry Christmas, Baby (Deluxe Edition)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026902f352fc980436434257e3"
                  },
                  "year": 2012,
                  "track_count": 16,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0umSoruyXd3wTna6hJTI4u",
                  "name": "You're The Inspiration: The Music Of David Foster And Friends (Deluxe; Live)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02477cc503c9305adb19dc4b4c"
                  },
                  "year": 2008,
                  "track_count": 13,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:4Cxmp9Oj3WaQ7kQaiQEvSH",
                  "name": "Sad Boys",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c1a889587e07814565f66479"
                  },
                  "year": 2023,
                  "track_count": 27,
                  "month": 1,
                  "day": 27
                },
                {
                  "uri": "spotify:album:3wFk9RAwkeqKQLflseLRnd",
                  "name": "Hits Cultes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0269e3ebfc2ab80c8fa7e1c8af"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:09re3n5zVk9V0WfiaQHAUs",
                  "name": "Hit Man David Foster & Friends",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0235390d7a12a74f6a1f1f73be"
                  },
                  "year": 2008,
                  "track_count": 9,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:4oHzpufroGd6M3AtzBGi57",
                  "name": "Ghost Ship Mellow 10's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0202d4c3ca9ac4da4f2a868503"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 5,
                  "day": 6
                },
                {
                  "uri": "spotify:album:0rzc6h8YRAjCzLy4tRF24s",
                  "name": "TikTok 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0283b122a28e29426a09e16ee3"
                  },
                  "year": 2023,
                  "track_count": 34,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:2JQkpF4O61P7YIZ361sUux",
                  "name": "All Out 20s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02463ff5b0130a5afdfb80ed4d"
                  },
                  "year": 2023,
                  "track_count": 89,
                  "month": 3,
                  "day": 15
                },
                {
                  "uri": "spotify:album:25IhVlc3V9ohYy3zHT9g5k",
                  "name": "100 Hits from the 10's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e58945230a15deaed1789808"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:2dDFlP577x12CyCjvwMjEX",
                  "name": "good vibes 100 happy hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d8fd895a45a52091faedf125"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:6S183X2AUTWpO6unjuiJ7T",
                  "name": "Chill Songs Everyone Knows",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026594cf5d04f66c1f2544c23f"
                  },
                  "year": 2023,
                  "track_count": 37,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:7rblblNvpqb8hQDOI9dGDT",
                  "name": "Covers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026fc659da8b544cf3e82c418c"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 1,
                  "day": 27
                },
                {
                  "uri": "spotify:album:3T3zAVw7TKPSqIZdhPC60s",
                  "name": "2000s Throwbacks",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0229e0099cfa8682b1ac443c6c"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 1,
                  "day": 27
                },
                {
                  "uri": "spotify:album:4YqphwrIOGbx2yJmu4sicO",
                  "name": "Skinny Love Classic 10's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d4835663d3d841989fb072ac"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:7DzEHnc4tSPmrc8irqup8l",
                  "name": "JULEHITS 2022 - Den bedste Julemusik og Julesange",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022049e5ea2a8282a789f09ddd"
                  },
                  "year": 2022,
                  "track_count": 51,
                  "month": 8,
                  "day": 26
                },
                {
                  "uri": "spotify:album:7wVN6T247gWq2rbqbhmdaN",
                  "name": "Chris Isaak Christmas Live on Soundstage",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c60f6587e3230569b40ec876"
                  },
                  "year": 2017,
                  "track_count": 17,
                  "month": 11,
                  "day": 10
                },
                {
                  "uri": "spotify:album:3A3PFd8gDsZwEzVgSz5jc9",
                  "name": "Hits of the 2000s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021d32d62f4928cd8d97e1fafb"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:3cgbFUyQthb1uU4uzOOpKg",
                  "name": "Coffee Shop Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0233ec0274c457ad57e7d5732e"
                  },
                  "year": 2023,
                  "track_count": 59,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:0QdGUF7cIU6B5mmIgBRo93",
                  "name": "21st Century Ballads",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02691acec521d4937f62d813c8"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 5,
                  "day": 17
                },
                {
                  "uri": "spotify:album:26A2ULZFg7bl0yOXqgRWio",
                  "name": "Feel Good Morning",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0205fbd811855eae05a41c0735"
                  },
                  "year": 2023,
                  "track_count": 39,
                  "month": 4,
                  "day": 27
                },
                {
                  "uri": "spotify:album:1QgAptWBHIULVzugDzWGNF",
                  "name": "summer songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02eb8b00faa6b10298041d5ead"
                  },
                  "year": 2023,
                  "track_count": 80,
                  "month": 4,
                  "day": 14
                },
                {
                  "uri": "spotify:album:7hOVYLDQ0G03J4lXVjpuoc",
                  "name": "Summer Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b1cb72c1a4206ef530986539"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:43dtonFZdnnJnENYzRwsVK",
                  "name": "POP LOVE HITS VOL 20",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b6aec3290151f37a7d028b65"
                  },
                  "year": 2023,
                  "track_count": 32,
                  "month": 3,
                  "day": 8
                },
                {
                  "uri": "spotify:album:2Txaj6moIytXUvr4vL94Kl",
                  "name": "Sounds of the 2000's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c95d800d483eddc9b75a4836"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:3FaRHG1jPq4YgsEB1aGyKU",
                  "name": "after ski chillout",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e2b50e4d0097d565e03779bd"
                  },
                  "year": 2023,
                  "track_count": 75,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:0HtgGgP6H0OP698Y8LAQjC",
                  "name": "Candle Light Dinner",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0248ea3fa327e6a85d1446f681"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 9,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3R58SQjcODgoe18I5BOoyS",
                  "name": "Kerst Top 40 - Kerst Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0298ae173e791a1f56603fde52"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 8,
                  "day": 26
                },
                {
                  "uri": "spotify:album:643FCFlLKLswiQ3F2tXSpa",
                  "name": "Anne Murray's Christmas Album",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02287ef4b3b56f720facac2921"
                  },
                  "year": 2008,
                  "track_count": 15,
                  "month": 1,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0NSLj8nCkPyUwdEkfaSjiJ",
                  "name": "Twenty Tens Easy",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028d4a16719adb05334e7037f1"
                  },
                  "year": 2023,
                  "track_count": 37,
                  "month": 5,
                  "day": 25
                },
                {
                  "uri": "spotify:album:2vLY56HF0wEJcP5sv593eL",
                  "name": "99 From the Noughties",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c1dafdcbe5ccf6057e835b7c"
                  },
                  "year": 2023,
                  "track_count": 99,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:1CuKFHajKnGPsvPB1Jvff4",
                  "name": "Summer Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e48e922421c704c1bed590e2"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 5,
                  "day": 11
                },
                {
                  "uri": "spotify:album:2uCuBiw8JfAQZ6eU8dbskA",
                  "name": "cuisiner en détente",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02383a66bd8836b7dfc1c6f5ad"
                  },
                  "year": 2023,
                  "track_count": 36,
                  "month": 4,
                  "day": 14
                },
                {
                  "uri": "spotify:album:1p53oP2NsrFNPBDzG9h4Fb",
                  "name": "00's Super Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b4de450a0ab1fa17f6081454"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:0lPwKX79Z4GFu9CpzBBksZ",
                  "name": "Duets",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ba9839ad6b8c3a26438b74d2"
                  },
                  "year": 2023,
                  "track_count": 48,
                  "month": 3,
                  "day": 8
                },
                {
                  "uri": "spotify:album:0sC0F7alp8hYCUqy0b5qQb",
                  "name": "soft & easy mellow hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027ee10390a421c6fd944ec3ee"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:0b96gF7rIA2AahNLzDdWim",
                  "name": "Love Playlist 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b0e35f3adc53a4310fd0ff7b"
                  },
                  "year": 2023,
                  "track_count": 43,
                  "month": 2,
                  "day": 10
                },
                {
                  "uri": "spotify:album:5qHgceN4XtuAKVR4ynhRDV",
                  "name": "100 Easy Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025d2149952b7a0c4e42feb5b5"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:1KNBgtiPjoKxO336F07aCJ",
                  "name": "Soft Pop Mix",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025bd1cca418e47f0166b04995"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:2iF91SUnIpjzNq9OnFCqfA",
                  "name": "Hits Online",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0204a08dd0f3e26f15ddd52a75"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3tuhdY8qOnhGYYyIoizLUG",
                  "name": "Celebrating Barbra Streisand On her 60th Anniversary with Columbia Records",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e414d23e5a3c2aa2664cf700"
                  },
                  "year": 2022,
                  "track_count": 24,
                  "month": 9,
                  "day": 22
                },
                {
                  "uri": "spotify:album:7Kc9ucc0TJwLluSODv2Lyd",
                  "name": "Rise Up - Inspirational Pop Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023931a58bf054a3da59864afa"
                  },
                  "year": 2022,
                  "track_count": 27,
                  "month": 7,
                  "day": 15
                },
                {
                  "uri": "spotify:album:1raUCvVAAig7CkqWUwYTrJ",
                  "name": "Wedding Party Playlist",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0269ddd7da78b9daa4dc91e702"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 5,
                  "day": 26
                },
                {
                  "uri": "spotify:album:2Nb9w5UVO32DxPV7FRRHDF",
                  "name": "All Out Easy Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027717e191d33e42f1a532ae5e"
                  },
                  "year": 2023,
                  "track_count": 75,
                  "month": 5,
                  "day": 5
                },
                {
                  "uri": "spotify:album:1T8U2CEx2pkHAYVumk7PaH",
                  "name": "On the Beach Summer Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02dc21dc9b3b81cab3b0643c22"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 4,
                  "day": 14
                },
                {
                  "uri": "spotify:album:4h0xw6sDOCFADZNttLoMyE",
                  "name": "Tú y yo",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02cd80639b8f7f297d626990a5"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:48lFfWYNBCGB1FvKI9K7QY",
                  "name": "Clocks - 00's Best",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02465e070cbb4a0da7c1003487"
                  },
                  "year": 2023,
                  "track_count": 70,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:4rRAefFaHb3j4aQUtZ0Xnr",
                  "name": "100 Love Songs Classic Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02073bd258dfe000c3a95cb7be"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 1,
                  "day": 27
                },
                {
                  "uri": "spotify:album:5IUHsw9NoHHnrxUPlbRfhV",
                  "name": "Break My Heart - Best Breakup Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02edb51497e6435eea3f32333c"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:2h66Q8ez8XvHFrRoWGjXyj",
                  "name": "Mellow Adult Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02eedc48ed86140ce3ec1dd481"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:2HU17SEYHEsx4AjjEHC0uE",
                  "name": "Hit Rewind",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e25d1cd025c51702486960fa"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:1zmGbaQ8VwFG8s0pqwybt0",
                  "name": "Throwback Latina",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02afe788eacf276fb5a461fc27"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:7AQYYGlH6JCm7eQ1TIvKtS",
                  "name": "00s Fever",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02169af5cb72abd4b6511fa16b"
                  },
                  "year": 2023,
                  "track_count": 70,
                  "month": 5,
                  "day": 26
                },
                {
                  "uri": "spotify:album:3UvkuW6w1Sg1daHugkElg3",
                  "name": "Good Morning It's gonna be a beautiful day!",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0246bc8421ff1acbdd4684e588"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 5,
                  "day": 19
                },
                {
                  "uri": "spotify:album:1iaGgbZ6x6xwjqisw3BynT",
                  "name": "THIS is the 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0200f538f86a8ed451dfbe1783"
                  },
                  "year": 2023,
                  "track_count": 80,
                  "month": 5,
                  "day": 17
                },
                {
                  "uri": "spotify:album:539CdfCyps85fJ6zvIaZuF",
                  "name": "Au Calme",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0254f009fc023b5508588aed16"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 5,
                  "day": 7
                },
                {
                  "uri": "spotify:album:3JYzcETYpn8a7TcJQlBNF1",
                  "name": "Out of Time Back to the 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029518b8712323d87d5814b1a2"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 5,
                  "day": 6
                },
                {
                  "uri": "spotify:album:6o6IdmyWIyJzpsHNi9oKMu",
                  "name": "20's New Era New Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0290cb53d57a449f4274be5299"
                  },
                  "year": 2023,
                  "track_count": 70,
                  "month": 4,
                  "day": 14
                },
                {
                  "uri": "spotify:album:4sGotzOclp0GsnQRC20Pzb",
                  "name": "Top Canadian Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022e983a6fa5ea675baa4b1ede"
                  },
                  "year": 2023,
                  "track_count": 26,
                  "month": 3,
                  "day": 11
                },
                {
                  "uri": "spotify:album:6wHk8Zctd97EVOUpXytOya",
                  "name": "Lugna Favoriter",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02002ec427e82e145832da06c8"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:2U4VJegEsN5FM0l9U1aMsb",
                  "name": "Easy 20's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020c142f1a59714dbbf07b19b1"
                  },
                  "year": 2023,
                  "track_count": 38,
                  "month": 3,
                  "day": 8
                },
                {
                  "uri": "spotify:album:3NqqyI4FuNmcIwlotB2Uzm",
                  "name": "Sped Up Songs - Fast TikTok Trending Viral Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f93d02cb41f5739571d7c637"
                  },
                  "year": 2023,
                  "track_count": 24,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:3FXbWukWwUyoKn5jJTtGCM",
                  "name": "Jazz Lounge",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0251101c513f7f8a625c9366e7"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:4R6BJjRtLlsl1rR6Ldla93",
                  "name": "Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c26ffacba17625fb5412db17"
                  },
                  "year": 2023,
                  "track_count": 62,
                  "month": 2,
                  "day": 13
                },
                {
                  "uri": "spotify:album:4omYlLTBPcrKUQ1gVm9vHe",
                  "name": "Love Songs 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026decdaa96f970494e7e8abed"
                  },
                  "year": 2023,
                  "track_count": 20,
                  "month": 1,
                  "day": 24
                },
                {
                  "uri": "spotify:album:1expt8qsm0GKSVOSC1ErZ6",
                  "name": "Heaven - Soft Pop Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0237e1dcf6f1fc9fa6204dd51f"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 1,
                  "day": 6
                },
                {
                  "uri": "spotify:album:02F8rrSRXF3WFh2lSzcNDC",
                  "name": "Wedding Songs - Marriage 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ccf4592191fab821e7c61921"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 12,
                  "day": 30
                },
                {
                  "uri": "spotify:album:6trVDKTqgqDQ5Y9xAHfPec",
                  "name": "Noël Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02eb947e89bc6949e862155473"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:1YDU8JDHG7xdWimuRRdAaf",
                  "name": "Pop Hits Now",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027d140f25b0a101cae46b7f06"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:7r59inlJtpX2cvJaneSJcV",
                  "name": "Impossibly Romantic - Top Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026a526deabf3c2b3a042e7547"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:4dSSI0Y0vO6BAIirtaczDc",
                  "name": "Jazz And Wine",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023752acf84a92cb526af4a4e2"
                  },
                  "year": 2020,
                  "track_count": 39,
                  "month": 7,
                  "day": 10
                },
                {
                  "uri": "spotify:album:1CVVdGw9GbLrmnXr7hFjGB",
                  "name": "Contemporary Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02495379f1441af3f9fcd7559a"
                  },
                  "year": 2023,
                  "track_count": 53,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:2PSTVuBCbM9dbK5Rfos32C",
                  "name": "Chilled 00s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02173529af2bf432257084f45a"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:5T2xjcM1fuXE8oBCChSzmy",
                  "name": "All 00s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fed45ae1862b8f3f666c00be"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 5,
                  "day": 25
                },
                {
                  "uri": "spotify:album:0mwbPVPFslPfKdJkDPONae",
                  "name": "Perfect Love",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0205df47bfd0b441d18f0163e6"
                  },
                  "year": 2023,
                  "track_count": 32,
                  "month": 5,
                  "day": 19
                },
                {
                  "uri": "spotify:album:6vv320N4TxCWRdikAno0Bi",
                  "name": "Safe Zone",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0236cdb1cd94126739b3fd37da"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:5JrKjAESUlOMdwfh3C7dso",
                  "name": "soft & easy",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022ee23a276ec87b9513cad79a"
                  },
                  "year": 2023,
                  "track_count": 75,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:7Lw4wCfrU7mmHX5BEp0p1a",
                  "name": "Lovesongs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fefacdf0332985a89b8ec44e"
                  },
                  "year": 2023,
                  "track_count": 33,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:7gpCtiwcQPbr2pSB9zk94b",
                  "name": "Koffietijd 100 Easy Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0298e0964be35bc1e98a24c0b9"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:3JYnUdrTfnXK30UMUEfqaJ",
                  "name": "Il buongiorno a colazione",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0249298b3835fb051f89bac5bc"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 3,
                  "day": 24
                },
                {
                  "uri": "spotify:album:7HMiCpjychhi26vh70dCfn",
                  "name": "easy evening mellow hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a5113e7aa4aa4b200475f4bd"
                  },
                  "year": 2023,
                  "track_count": 65,
                  "month": 3,
                  "day": 24
                },
                {
                  "uri": "spotify:album:04MgoZ58PSAMXh5IXnjGfG",
                  "name": "On the Road",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02855db8f5aded0ecb0da20e33"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:6m1eqRpl6rleiDjdsWiaS4",
                  "name": "Epic Soundtrack Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02cf684e1f677ff25e99eaec9a"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:6WDo5Gv6cvg2B9ebbyyYW9",
                  "name": "Sweet Pop Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020f4455fcbf08004ca3212d9c"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 12,
                  "day": 23
                },
                {
                  "uri": "spotify:album:1PSb3CFUo5eNvtdlsJqmgS",
                  "name": "TikTok Christmas Viral Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0226a8333b94d0e14519effd46"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:2Lo3cqrtr8TL9OJFoaND4Z",
                  "name": "Frühlingsgefühle",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021c8250c50fb4556a8dfc9080"
                  },
                  "year": 2023,
                  "track_count": 33,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:2DipremhYqYkajV2Gqf0UV",
                  "name": "Coffee and TV Lazy Mode",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02504332d4f1a0512131603aab"
                  },
                  "year": 2023,
                  "track_count": 85,
                  "month": 5,
                  "day": 6
                },
                {
                  "uri": "spotify:album:5PTYG2NexcWKF7Qsva85Q6",
                  "name": "My Romantic Soundtrack",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0207cf0689a435e89dac7552ba"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 3,
                  "day": 24
                },
                {
                  "uri": "spotify:album:4kpWAIY0tURXMLUjzG2oUd",
                  "name": "Clocks - Viral Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ef84e2121f4bf657094e3c6f"
                  },
                  "year": 2023,
                  "track_count": 44,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:2C6VGUvrv5MGrJPDxZ7emh",
                  "name": "20's Biggest Bops",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026b64fd830e9bab482a26678d"
                  },
                  "year": 2023,
                  "track_count": 79,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:4m03tKteg2F7us917ySL1u",
                  "name": "TikTok Viral Hits November 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028c6a9ac05d30996a9334f1e8"
                  },
                  "year": 2023,
                  "track_count": 44,
                  "month": 2,
                  "day": 20
                },
                {
                  "uri": "spotify:album:27WFuRkXlff3IO6coq8j1Z",
                  "name": "Some 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0234ca06db59984ab33045477b"
                  },
                  "year": 2023,
                  "track_count": 28,
                  "month": 1,
                  "day": 13
                },
                {
                  "uri": "spotify:album:6HddYKQx5ctqxiq8t4biHz",
                  "name": "Sylvester Party Hits 2022 / 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0281703208698fba7a0a67d837"
                  },
                  "year": 2022,
                  "track_count": 78,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:3MqKbbresk722CGYVhMBTm",
                  "name": "Coastal Grandmother Summer",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0286b3b0a06e851ffc776c5905"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:4jlCOCyREV3GPTWk7yCvbH",
                  "name": "Summer Party 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a8108100b7f769239dc77bbe"
                  },
                  "year": 2021,
                  "track_count": 61,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:3y9Lju9gXryxC5RH4BJ5AM",
                  "name": "Trending Christmas Hits Volume 1",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026a074e21e54d144a88989bec"
                  },
                  "year": 2020,
                  "track_count": 216,
                  "month": 11,
                  "day": 27
                },
                {
                  "uri": "spotify:album:0AcwmsnhxnFm4UUwlwtStB",
                  "name": "Christmas in July 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0234d2cc1a7b67be9627a1b202"
                  },
                  "year": 2023,
                  "track_count": 45,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:4lNuaBYJLy29AZntc2XJ4W",
                  "name": "Weihnachten 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029eba388246cafc7b05a837ab"
                  },
                  "year": 2023,
                  "track_count": 62,
                  "month": 5,
                  "day": 19
                },
                {
                  "uri": "spotify:album:16jBQjeUxHJZZRq0xhnGDz",
                  "name": "Easy Listening",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0236fd9b53b055a9ebbef6ea98"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 4,
                  "day": 21
                },
                {
                  "uri": "spotify:album:1D3vPi3f9Uo5LstxkSovHd",
                  "name": "Rustige Muziek",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0249ac06e0d736740a71c81911"
                  },
                  "year": 2023,
                  "track_count": 45,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:1ToGNJXEFT6wxJbiqBkdRT",
                  "name": "Musica per rilassarsi",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020f58daa969e19dcef296a840"
                  },
                  "year": 2023,
                  "track_count": 42,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:6x6iIQD5p5kHnmrH0iD2z3",
                  "name": "love is in the air",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0255bd796711d0b45edd2e864e"
                  },
                  "year": 2023,
                  "track_count": 70,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:5pIEFFwOUjgbQtY1GzhoXH",
                  "name": "This Is Pop - 100 Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028e7bdcbb5e6d966852045d18"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 2,
                  "day": 20
                },
                {
                  "uri": "spotify:album:4YvtFWnHVypecyN6ITUiCW",
                  "name": "Tik Tok Viral Hits Dezember 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b36012e8ac0adc4a0a0cd064"
                  },
                  "year": 2023,
                  "track_count": 47,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:5tTUCKifxzEuBaqCH45X6F",
                  "name": "10's Easy",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02310407ee3d15676f842473bd"
                  },
                  "year": 2023,
                  "track_count": 47,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:4N3bmtOQxfR6qsw2aK6DyW",
                  "name": "Absolute 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026ebfea7aecbc99b49dba05bd"
                  },
                  "year": 2023,
                  "track_count": 100,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:1ayhZEMVEnAdoWY8AJFDkm",
                  "name": "Teen Beats - POV",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0275fb876b967adfc78fa2e356"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:5apSpNV3At1DlEM119g4yj",
                  "name": "Auto Meezingers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026e4b2c4f31d1032ec033874c"
                  },
                  "year": 2022,
                  "track_count": 99,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:6RZD0Yk4YxP0TDieHPqCYX",
                  "name": "Weihnachts-Hits 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02749801ced465b72e0464f357"
                  },
                  "year": 2023,
                  "track_count": 61,
                  "month": 5,
                  "day": 19
                },
                {
                  "uri": "spotify:album:0q9tYv4xuLn4jBz1j1nrKg",
                  "name": "All Summer Long",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02916e8dede754dd30b67983dc"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:52krSwiJ689lyAWxcxn20l",
                  "name": "Ballads Classic Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021eddbce19951945a7a1aefc2"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 4,
                  "day": 27
                },
                {
                  "uri": "spotify:album:3X6GzQ1MNxSahGcTFnb5mS",
                  "name": "mellow pop hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02950fc4c954870d6e85b27ae0"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 3,
                  "day": 24
                },
                {
                  "uri": "spotify:album:3Sp8yVHaJXthiCFiLBtBDW",
                  "name": "Planet Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d22bb376d572977730b7f7c1"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 3,
                  "day": 18
                },
                {
                  "uri": "spotify:album:4nQ0izcYGC32AVYUNGge3i",
                  "name": "Some Duets",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02afbe42c5992223bbcd2d7ca0"
                  },
                  "year": 2023,
                  "track_count": 46,
                  "month": 3,
                  "day": 8
                },
                {
                  "uri": "spotify:album:5VI3shCgp5KyJMWCxU196N",
                  "name": "Better Alone: Songs to Listen on Your Own",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02aa42602a4104bf4f2d51eb0c"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:53tAkt968kQRIT9B3jeihe",
                  "name": "some 10s hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d558fd35ac1744b151492390"
                  },
                  "year": 2023,
                  "track_count": 69,
                  "month": 2,
                  "day": 20
                },
                {
                  "uri": "spotify:album:5pgvG3OP64g1FpTyBCQuoB",
                  "name": "00's Easy",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029cb97a6f3afb070abc9ce15c"
                  },
                  "year": 2023,
                  "track_count": 42,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:7CXeUqRBB5t4RA9kQB1W0i",
                  "name": "Valentinstag 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a81c1b476891d1a8737b2ab1"
                  },
                  "year": 2023,
                  "track_count": 32,
                  "month": 1,
                  "day": 24
                },
                {
                  "uri": "spotify:album:6HOXVMthAX8VidcsAAObKw",
                  "name": "Liebeslieder 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027a4076ce73ebd3cee1b5b61a"
                  },
                  "year": 2023,
                  "track_count": 20,
                  "month": 1,
                  "day": 24
                },
                {
                  "uri": "spotify:album:76KzZwjuaDTufhoJv4EHBN",
                  "name": "Dinner & Wine",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027be526142c0263348e4b0119"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 1,
                  "day": 6
                },
                {
                  "uri": "spotify:album:5lrKlSUvIvTXecYDGfsjGM",
                  "name": "Sway - Old Classics, New Versions",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d19a322eab2a32896f7fb94a"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:4qYoNCEXDoJkHpHHNflxpB",
                  "name": "Power Pop Ballads",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028e2e4fe8c9e1f7d2d93d2c3e"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 5,
                  "day": 27
                },
                {
                  "uri": "spotify:album:653DFnzmvoLbXySV1hDHBI",
                  "name": "Taking A Chance On Love",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d98e7cedbc8ca14f97ca217c"
                  },
                  "year": 2004,
                  "track_count": 14,
                  "month": 9,
                  "day": 14
                },
                {
                  "uri": "spotify:album:6YRlrm2Z9olvnF7yRhlsfZ",
                  "name": "Ballads of 2000",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029ccd38aac5ca1ea97278e392"
                  },
                  "year": 2023,
                  "track_count": 45,
                  "month": 6,
                  "day": 3
                },
                {
                  "uri": "spotify:album:1K8GMpwSiLdricD64WhaYv",
                  "name": "Velocità supersonica",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e99b116d20603cd41902b7a4"
                  },
                  "year": 2023,
                  "track_count": 13,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:5UHaESnmFHZh42lci0sh7X",
                  "name": "Pop 2K",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024bbeaaeeda1a60c14e20513f"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:6bO6oQMwX4vkME3qw1L3nX",
                  "name": "Hot Chocolate Mood",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028950eb0edc3b6854e4691893"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 4,
                  "day": 28
                },
                {
                  "uri": "spotify:album:6ylghKuKjgFBY96WD2H73A",
                  "name": "Love Pop Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02915dc4d1cc9539756a3ea7fb"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 4,
                  "day": 7
                },
                {
                  "uri": "spotify:album:3ayHHt4r7HvC2Q64ZiJuHL",
                  "name": "Broken Heart Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ebf35ab882a40bb3831e2074"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:0eXGgmdBspvvmNOsFtyknS",
                  "name": "Sounds of the 10s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fd99532ca966b86d2ac3b435"
                  },
                  "year": 2023,
                  "track_count": 69,
                  "month": 2,
                  "day": 23
                },
                {
                  "uri": "spotify:album:7IyGR53sO4P6j7vwfi8bwL",
                  "name": "TikTok Viral Hits Oktober 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d6794fdf986b07c101420f24"
                  },
                  "year": 2023,
                  "track_count": 48,
                  "month": 2,
                  "day": 22
                },
                {
                  "uri": "spotify:album:0TCnJS5gGcUMCbtXaBjDsW",
                  "name": "Valentijnsdag 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022638d1670e373ff1129b914b"
                  },
                  "year": 2023,
                  "track_count": 42,
                  "month": 2,
                  "day": 10
                },
                {
                  "uri": "spotify:album:7BOcqgGVZ0DA6Ydk6dEQt5",
                  "name": "Weekend with Family",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0291372fdecb3da571e4f8c51f"
                  },
                  "year": 2023,
                  "track_count": 32,
                  "month": 2,
                  "day": 3
                },
                {
                  "uri": "spotify:album:4WKYMKNzOkZIgT984sHokm",
                  "name": "Hits from the Movies & Television",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0291ff204e1976bba5bd9af04c"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 1,
                  "day": 25
                },
                {
                  "uri": "spotify:album:2GDmXhMrXx8c7bKDukw6xK",
                  "name": "Viral Internet Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022a7cdacfa80631aa96e5636a"
                  },
                  "year": 2022,
                  "track_count": 37,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:1fe8oHgSrXcKIeQCwOLHWe",
                  "name": "Evergreen - Viral Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0283e5592b068939fcf11c975e"
                  },
                  "year": 2022,
                  "track_count": 42,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:3LPghlMCICkwGHz3ab7U8R",
                  "name": "100 Greatest Christmas Songs Ever",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025a891d35be0de7ae2fb01130"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:6qy7AGji2tQKEbP6Dero2l",
                  "name": "Sunny Day",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ad2721d990e1561b9f192f3e"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:3j6xy3RVKiGxpgexbmFW2d",
                  "name": "Christmas Jams",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026572bbf47aea0e38c50646d9"
                  },
                  "year": 2021,
                  "track_count": 16,
                  "month": 11,
                  "day": 12
                },
                {
                  "uri": "spotify:album:1SLhly2T8NUFbkTTqhDCIr",
                  "name": "Pop 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02757f9f819f021775f0a59b2b"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:3Jh65quIhXk4o0SZaKeiam",
                  "name": "warm and fuzzy: happy pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021f569445145ad67f091a33c6"
                  },
                  "year": 2023,
                  "track_count": 34,
                  "month": 5,
                  "day": 26
                },
                {
                  "uri": "spotify:album:1mrX7tw5gaLBFkHauaCGgS",
                  "name": "Señora Cool",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c70d2d2907bfdef3931310fd"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 3,
                  "day": 17
                },
                {
                  "uri": "spotify:album:0DIVaOFsLdx0BdUlysYFyD",
                  "name": "You're Simply the Best",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023b2a1d0134ad36fcd122188d"
                  },
                  "year": 2023,
                  "track_count": 17,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:7xwIEH7BIYKH4ha5HNPnSZ",
                  "name": "Top Stars: Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021f0ea395129854be2961d2db"
                  },
                  "year": 2023,
                  "track_count": 27,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:511U26m5j5ymByi2P1bEgQ",
                  "name": "time capsule the 00's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0204e80fb60c8560f7f83d4598"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:0atEjLLWg7tOFS2jItJxKg",
                  "name": "Soft Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022da5b73712456a72920ebc3f"
                  },
                  "year": 2023,
                  "track_count": 39,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:4mBdnes2OEp1GufEBKbPXG",
                  "name": "Flowers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0203c95037854b14ed16f60a6d"
                  },
                  "year": 2023,
                  "track_count": 34,
                  "month": 2,
                  "day": 10
                },
                {
                  "uri": "spotify:album:2VMacTFnDCYLvEvAH1tkQF",
                  "name": "Happy New 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02284f81739b4ac6408b8c43a1"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 12,
                  "day": 30
                },
                {
                  "uri": "spotify:album:79edhnFwe5f1aNDqyP7gcj",
                  "name": "Silvester Party TikTok Hits 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0234ec48afdb1dfe72528785aa"
                  },
                  "year": 2022,
                  "track_count": 71,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:4cHruOJf2jxVhjc5VhA2I4",
                  "name": "villiain core",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0208884548ea5b911f4dbeee9e"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:5ADLt4rIizsbXcXnrKWZWO",
                  "name": "Modern Christmas Songs 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022e8a705cad731fa7acf69923"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 11,
                  "day": 24
                },
                {
                  "uri": "spotify:album:75PwXLArIPG6ctgaJbn3uz",
                  "name": "Christmas Music - 100 Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0226466026f863de3d921e0d67"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0424i9pWwCOiaAATz4xSxM",
                  "name": "Sounds of the 00s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b0b1e2db208e3503103cf943"
                  },
                  "year": 2022,
                  "track_count": 64,
                  "month": 9,
                  "day": 9
                },
                {
                  "uri": "spotify:album:2aZMmI1tb3AeH89VL4RHMu",
                  "name": "1, 2, 3 - Viral Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0245b19ffabc297475f36dcbcd"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 9,
                  "day": 2
                },
                {
                  "uri": "spotify:album:0z6k21hEbuHzcwm8dU98tD",
                  "name": "The Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0200f0f5f05ded5dc2ae179b40"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:6Ylp3xiy4nbkTjRLhwBkjJ",
                  "name": "I Don't Care: Best Pop Duets",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a0dd2c184c461f28044e46c0"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 6,
                  "day": 17
                },
                {
                  "uri": "spotify:album:4h5vQIaXWfFMFQTQCjPXHq",
                  "name": "Cocktail Party",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028b5c0f3fb5ecdcce318fd3c3"
                  },
                  "year": 2020,
                  "track_count": 33,
                  "month": 7,
                  "day": 30
                },
                {
                  "uri": "spotify:album:5OaCLmLOOsxv15TjOlQh5c",
                  "name": "Just Good Music - Wednesday Blend",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0224a20fb139ce3a5477862c79"
                  },
                  "year": 2023,
                  "track_count": 35,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:7iQ8e11nYltDJ7pEFS9o4u",
                  "name": "The 00s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b52517a7d8a2821fd2fdc9db"
                  },
                  "year": 2023,
                  "track_count": 50,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:1x6Lz4VGRV4g5WIOi4vv1E",
                  "name": "Amor de mamá",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c11008df535d4fb1dd78037e"
                  },
                  "year": 2023,
                  "track_count": 20,
                  "month": 5,
                  "day": 11
                },
                {
                  "uri": "spotify:album:14vm7o0onJik95uZlnRs4G",
                  "name": "Liefdesliedjes 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ebb70ee379d58b041ccbff76"
                  },
                  "year": 2023,
                  "track_count": 42,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:7wMJvFnsFSpF5qxHsZEwEN",
                  "name": "Alleen Liefde 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024b21f1d7fcde3dc5a95f480c"
                  },
                  "year": 2023,
                  "track_count": 43,
                  "month": 2,
                  "day": 10
                },
                {
                  "uri": "spotify:album:4xGCFO2kyqOv9UZZM5SyjU",
                  "name": "love songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0200ccd9e307b6802079b9802d"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:0pXVXyWeCefpGau8bayyeH",
                  "name": "Groove House Anthems",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02804ad4ddd3ee5b3eebc911b0"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 11,
                  "day": 18
                },
                {
                  "uri": "spotify:album:03b4Au3dymuJUz2YlAURcS",
                  "name": "difficult tiktok hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023ecc1ce8981c10c5a5dc67c8"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:5s6ucJt8QPE8JUPWb2neqH",
                  "name": "Psycho Killer - Virals",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024a0f6f3d616158d5a9541b59"
                  },
                  "year": 2022,
                  "track_count": 33,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:6VkA7VpvZzgGFVFIv0XOye",
                  "name": "Wedding Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020d23e9206733cb9d303abb40"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:6SitlgAoOBv1tpYmVC4Mvu",
                  "name": "Christmas Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fd87ef788d2bdb91db81045b"
                  },
                  "year": 2020,
                  "track_count": 206,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:6nHosu2ZD6FBJAyzqfmBe4",
                  "name": "Merry Christmas Eve",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0294c962ba9a29708d0abfbabf"
                  },
                  "year": 2020,
                  "track_count": 30,
                  "month": 12,
                  "day": 4
                },
                {
                  "uri": "spotify:album:4QPliGQlwNIVRrf7NQ1UkT",
                  "name": "Holiday Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0276c54d781994bfdc4175322c"
                  },
                  "year": 2020,
                  "track_count": 208,
                  "month": 11,
                  "day": 13
                },
                {
                  "uri": "spotify:album:0Cjls2vJjGLPI1OsswxR8u",
                  "name": "Christmas Party 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0200ef4444223c3d9ec71fcea5"
                  },
                  "year": 2020,
                  "track_count": 28,
                  "month": 10,
                  "day": 16
                },
                {
                  "uri": "spotify:album:7veXe4IUia6ZDvdcX691sS",
                  "name": "Dizzy - Summer Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02cbb90fe2d37b4a9d866e66ba"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 3,
                  "day": 10
                },
                {
                  "uri": "spotify:album:0fO3et4GycZqZ0FphwCtZR",
                  "name": "No Stress",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0244883d355e11b8b76c66db42"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:6sZXXfeVoJ1XojHsz7931O",
                  "name": "Just Best Covers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020bedd439dc3c07fec383b073"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 2,
                  "day": 23
                },
                {
                  "uri": "spotify:album:7qbuOmptU4ahaTg3E3Ci1E",
                  "name": "Viral Hits Trending 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027ff7b211848b210b7b5723f2"
                  },
                  "year": 2022,
                  "track_count": 44,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:5Y8zmloummaCJxKIlO7pVv",
                  "name": "Victorious 10's Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0286c956955be8ef025d692a03"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 11,
                  "day": 18
                },
                {
                  "uri": "spotify:album:4ZWoMk06dUmAF5jH06OEvU",
                  "name": "Merry Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c0d97b0f90505a02453eca7c"
                  },
                  "year": 2022,
                  "track_count": 84,
                  "month": 10,
                  "day": 31
                },
                {
                  "uri": "spotify:album:59Kw96zJdQEI8sXN4B0TzQ",
                  "name": "Weihnachtslieder 2022 - Bekannte Weihnachtslieder",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026959d0b242931f0aab476d84"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:10NkpNEVXABoma9akEZ73d",
                  "name": "Feeling Good - Adult Pop Favorites",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0233cfccec64c80f34a420d60f"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:0TfqrsPnjt2xgFT7nrGy5I",
                  "name": "It's Beginning to Look a Lot Like Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ca8d32258fb42def3a41fd36"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0tF4ZtEJ46RiUkgpmW4scm",
                  "name": "Imagine - Soft Pop Favourites",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fc4ba8cf299a6cb5a66bca82"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 7,
                  "day": 15
                },
                {
                  "uri": "spotify:album:0RjvkesUsDyJ29G506swpR",
                  "name": "Weihnachten 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c3f3fea1e154dfb9bfb56430"
                  },
                  "year": 2021,
                  "track_count": 21,
                  "month": 11,
                  "day": 19
                },
                {
                  "uri": "spotify:album:5sWwQPaXXoDcVufSoQeNH4",
                  "name": "Trending Holiday Hits Volume 1",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02406aebef8f41c48e1bda0268"
                  },
                  "year": 2020,
                  "track_count": 216,
                  "month": 11,
                  "day": 27
                },
                {
                  "uri": "spotify:album:6jGHZMwGCvcjIL7JUAM38f",
                  "name": "Holiday Cookie Mix!",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e37a2ab88aa140e2fea3eb29"
                  },
                  "year": 2020,
                  "track_count": 208,
                  "month": 11,
                  "day": 13
                },
                {
                  "uri": "spotify:album:3Dlz0ZNSqXONqDyMRGwuh8",
                  "name": "Dreams",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c0be9909954222fe45446ca5"
                  },
                  "year": 2023,
                  "track_count": 20,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:2eU4OFjlMn0x7tbgZ8HaiP",
                  "name": "Just Good Music - Monday Blend",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026af1de9349caf6023410e5ce"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 6,
                  "day": 2
                },
                {
                  "uri": "spotify:album:2uDY7IGD0a5RNZoKAniwx2",
                  "name": "Cocktail Evening",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a6b01fded42544ddbb0d948a"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:6lcAM6Q0VbSnzCA7wAir8i",
                  "name": "Habits: Big Virals",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02372230cb93fa08c342e6f504"
                  },
                  "year": 2023,
                  "track_count": 37,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:5UddH6MDN5JoZU7epw607L",
                  "name": "Winter Chill",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02795d1584bdbdabe4d46dd435"
                  },
                  "year": 2023,
                  "track_count": 24,
                  "month": 2,
                  "day": 24
                },
                {
                  "uri": "spotify:album:6HlEmUbf7XwHgx4kEaxg6C",
                  "name": "Feeling Good - Jazz Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ad3e1dcd80b5df6a4f413c1b"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3hgwoY46Ud7MdJQHAWv9ll",
                  "name": "Kerst Vibes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022486250a114654d41c8f234c"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:0bVOp6pJiWAw45RIhcvKvl",
                  "name": "Before You",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025c939e8dff75361b5b4546b1"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:3IPAeTsbI5kf1IwItt2EqH",
                  "name": "Holiday Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02402e18b9fd717d30c9ed96b5"
                  },
                  "year": 2022,
                  "track_count": 56,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:2o6GuaSdpIBlZYJZxWYINu",
                  "name": "Best Christmas Hits Ever",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0234f080a3ee04d64d92a4c2e5"
                  },
                  "year": 2022,
                  "track_count": 80,
                  "month": 11,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3TtJKgjWFv6FCpoySV7ofv",
                  "name": "B.O.T.A. - Viral Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f015df292f2315b2976934ff"
                  },
                  "year": 2022,
                  "track_count": 33,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:6zGNFXI4m0ujlFQa3Fuagr",
                  "name": "Human - Best Adult Pop Tunes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0283a4a00a95e85f9961caa31b"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:06P85maDUL4tV0qHux0vDg",
                  "name": "Take On Me - Viral Throwback",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02dda4dff06672b27d4428ff82"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:6T4mIPZdNbjbdUeRpntjbU",
                  "name": "New Christmas Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0269b87eb3685edf8652a121b1"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:3rk3DghJCaJctKeayJHGJH",
                  "name": "Kerst 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021d8ae95a91d13036941dc6cb"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 9,
                  "day": 16
                },
                {
                  "uri": "spotify:album:1lXmkZphwtIDmJ5iDu7wBa",
                  "name": "Relaxed Vibes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0213ee058606ff23fdf3b53a1c"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 7,
                  "day": 29
                },
                {
                  "uri": "spotify:album:0mYctElmuOG5tWsT5T3jaL",
                  "name": "Pop Joy",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b694aa7009b2e59428e8a66c"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 7,
                  "day": 15
                },
                {
                  "uri": "spotify:album:5xmv67qE17rUWyFBDCOe3b",
                  "name": "Softer: Pop to Chill",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b8dffa6fdd4f073e50cdbd13"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 6,
                  "day": 24
                },
                {
                  "uri": "spotify:album:5hejbQwKmx8CSvwa7HVkYw",
                  "name": "The Best Christmas Album In The World...Ever! 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027736e5a03b30b0942c73e019"
                  },
                  "year": 2021,
                  "track_count": 69,
                  "month": 11,
                  "day": 9
                },
                {
                  "uri": "spotify:album:7kRlSIjucX0C5SroPcm8iB",
                  "name": "My Jazz Music Café - Espresso",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02343df79754b920ba893b7379"
                  },
                  "year": 2020,
                  "track_count": 15,
                  "month": 11,
                  "day": 13
                },
                {
                  "uri": "spotify:album:0sJKd292wKgIkn9cYD8Qje",
                  "name": "Pop Para Navidad",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0250fccde390acf3177aea1c85"
                  },
                  "year": 2020,
                  "track_count": 37,
                  "month": 10,
                  "day": 23
                },
                {
                  "uri": "spotify:album:3jvLSANFelFMsYULUyBY71",
                  "name": "Love From King's",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025c4bf999a56d7ebec02153a6"
                  },
                  "year": 2018,
                  "track_count": 14,
                  "month": 2,
                  "day": 2
                },
                {
                  "uri": "spotify:album:0p0KK0pqqxBImESQ0YEN0f",
                  "name": "Mental Health Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c120ba95169d1d33d9c1625c"
                  },
                  "year": 2023,
                  "track_count": 20,
                  "month": 5,
                  "day": 12
                },
                {
                  "uri": "spotify:album:6vUpnyCgSEnZ25SNrvHEAc",
                  "name": "Easy Favorites",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028f5258af6c1bc441a5bcd93b"
                  },
                  "year": 2023,
                  "track_count": 40,
                  "month": 3,
                  "day": 24
                },
                {
                  "uri": "spotify:album:5ZLRywdGpwvjKmR51tfNen",
                  "name": "State of Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a27223091e4ec416cb121d49"
                  },
                  "year": 2023,
                  "track_count": 49,
                  "month": 3,
                  "day": 3
                },
                {
                  "uri": "spotify:album:0t0qHan811sGZFFktrLrdQ",
                  "name": "Kerstmuziek",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c42b36b171454d75bfac4daf"
                  },
                  "year": 2022,
                  "track_count": 100,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:7mdxLskn9P5OyrzDpA1PBw",
                  "name": "Christmas Workout by Pamela Reif",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a2d5d0fc0882d8fa768b83f4"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 12,
                  "day": 14
                },
                {
                  "uri": "spotify:album:3Dy0PnFTBToW4PfIskM0Il",
                  "name": "Santa, Can't You Hear Me",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d8208861ef5c17abd2b601bc"
                  },
                  "year": 2022,
                  "track_count": 56,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:2CYyMtT85d8fB5VKPqHXfJ",
                  "name": "Jazz Navideño",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fb0a483e761ff9ca649d7ba2"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 12,
                  "day": 8
                },
                {
                  "uri": "spotify:album:2jaxorgTL3OejEkYeRUFoj",
                  "name": "Christmas Lights",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020bae8153daa081fcca1ed6e6"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:2wzuNVwmF3JNi89pdRwEux",
                  "name": "Christmas Romantic",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022471a0ab6b4c6331c2cc6d83"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:3XF3wYZTbdN2MABmIWCT8m",
                  "name": "Dinner & Wine Holiday Edition 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023f3ba1c764cb2e5530232e9e"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:6Qqq1nymtanZY6ILleDZpO",
                  "name": "Family Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f2b541b1a1cb65d7d07d6aec"
                  },
                  "year": 2022,
                  "track_count": 36,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:3rc7XH8LOF5yLuBMIapsp3",
                  "name": "Christmas Road Trip",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02392a90d4d174710afe680212"
                  },
                  "year": 2022,
                  "track_count": 64,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:3ZA2dVknJjIziZQVuI1nB9",
                  "name": "Best Christmas Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028ae5ccf35262d10c5beca504"
                  },
                  "year": 2022,
                  "track_count": 94,
                  "month": 10,
                  "day": 27
                },
                {
                  "uri": "spotify:album:59z4CNPuuXCl3r0DsMG2MS",
                  "name": "Cold December Night",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b8bbff61d69fa4a038521c59"
                  },
                  "year": 2022,
                  "track_count": 64,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0jhyOgAfYKaIvKBMkx2hQU",
                  "name": "Natale 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fe11ce8136019c1349a46539"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:3gzqPZ4etezunKyQi2aByL",
                  "name": "Weihnachten 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021a5ce4599077d7e03a6ffd8e"
                  },
                  "year": 2022,
                  "track_count": 29,
                  "month": 9,
                  "day": 23
                },
                {
                  "uri": "spotify:album:5yxUMS3dOgijdQQfPOcZJc",
                  "name": "Trending Again - Classic Hits Back on Top",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02868e78839efddb5852320f00"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 9,
                  "day": 9
                },
                {
                  "uri": "spotify:album:0GT81dvLd8ouzFc0JgUWYk",
                  "name": "Feel-Good Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0215129ea9495a944527a1783e"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 8,
                  "day": 19
                },
                {
                  "uri": "spotify:album:4c4HQ0ByF3VcsP3WetzctR",
                  "name": "Ready: Pop to Start the Day",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0229aca648d3a6bcdd2c14ab61"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 8,
                  "day": 12
                },
                {
                  "uri": "spotify:album:0ApsDPvXbaX2d0FvnsAhRl",
                  "name": "Good Vibes 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022d08efcc56d15aa749e71797"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:7nWXJzBt8M1uhDTxY9iT1r",
                  "name": "Sway - Hits Today",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ef29d2938be0cadb4a3e4290"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 7,
                  "day": 29
                },
                {
                  "uri": "spotify:album:6oaEAZTCTGofMvsLF7m6LI",
                  "name": "暖心聖誕 Cozy Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02343ed992eb340d711f8e89cd"
                  },
                  "year": 2021,
                  "track_count": 36,
                  "month": 11,
                  "day": 17
                },
                {
                  "uri": "spotify:album:4wyopDrPXbT7iKLb7gQMmA",
                  "name": "Merry Christmas - God Jul",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023065d9b6d89f1ac7001b25d0"
                  },
                  "year": 2021,
                  "track_count": 103,
                  "month": 10,
                  "day": 8
                },
                {
                  "uri": "spotify:album:7ABc0LJsVt1ZuMFtgZh5wW",
                  "name": "Holiday Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027bb3e0d748d15f619612b721"
                  },
                  "year": 2020,
                  "track_count": 206,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:21p1nPhQ8fFp29KiKQ3ijX",
                  "name": "Christmas Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b17ef514122785b3d1899321"
                  },
                  "year": 2020,
                  "track_count": 206,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:0NjluZYW6UBKYSyuNVdTPi",
                  "name": "Invierno Musical",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bcb1aba805816ab7dab0f9c1"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 6,
                  "day": 9
                },
                {
                  "uri": "spotify:album:4OErYkFuaBkvIm7iS9aBEr",
                  "name": "Canadian Gold",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029e29240044a533e812bb3350"
                  },
                  "year": 2023,
                  "track_count": 31,
                  "month": 3,
                  "day": 31
                },
                {
                  "uri": "spotify:album:7eaz3bi7sPjNBjFLsDLQ3X",
                  "name": "Before You - Love Songs 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021a8c241599ea592c8dbf7164"
                  },
                  "year": 2023,
                  "track_count": 27,
                  "month": 2,
                  "day": 17
                },
                {
                  "uri": "spotify:album:2BkPix2fFaCug7ep94aktF",
                  "name": "Let It Snow - Winter Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0200cbff86c729e995483e06d1"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:18L5sNMEhR0NpnrHQkbqqw",
                  "name": "Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0281768780bef2f3a1efee013e"
                  },
                  "year": 2022,
                  "track_count": 33,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:1jDw3movNtW4jQtOclFIzM",
                  "name": "Weihnachtsfrühstück",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0239acd6b2a1c0d4f84b5973c5"
                  },
                  "year": 2022,
                  "track_count": 33,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:1rsOu13mMMV7eZ7YGTUsVW",
                  "name": "Retro Modern Pop",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024dd7cda9e1009c222ae43263"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:07Lbu35V6H6hCp6xfS1VU7",
                  "name": "Christmas Love Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023a0ce421559ed065827e4233"
                  },
                  "year": 2022,
                  "track_count": 21,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:7obUkLsm4NUeXv97PWhnp2",
                  "name": "Let It Snow",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020c8299b924497009e192af77"
                  },
                  "year": 2022,
                  "track_count": 70,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:5E63Ph4ECryJkJBEukJb0K",
                  "name": "Country Holiday",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02df66b31207f7998b3acb8214"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:43EvDpZ1yfb4hKxS37HVsX",
                  "name": "Holly Jolly Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0256c3544e4cf83b2ec71d63ff"
                  },
                  "year": 2022,
                  "track_count": 65,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:64ZX0KzU9v94siCXlGmm4P",
                  "name": "Mistletoe & Wine - Christmas Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f2c6812f34170fa6eb43ec80"
                  },
                  "year": 2022,
                  "track_count": 70,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:7kkSf2E3MsJrkPAJfxCqOJ",
                  "name": "TikTok Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022d74bd4707318ae8912674ac"
                  },
                  "year": 2022,
                  "track_count": 71,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2ruNpUYGBxAb8PRekt9LP0",
                  "name": "Weihnachts Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02efc7dd0f874364d5ea45b452"
                  },
                  "year": 2022,
                  "track_count": 59,
                  "month": 9,
                  "day": 23
                },
                {
                  "uri": "spotify:album:4U0pa45igbVYmFGrdgRwmV",
                  "name": "Pop Shivers",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b84a5da22266107b9ef6f638"
                  },
                  "year": 2022,
                  "track_count": 26,
                  "month": 8,
                  "day": 5
                },
                {
                  "uri": "spotify:album:6tTaVzpv5a3fuornlx5F4a",
                  "name": "Teen Beats",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029263ac5827dd484855926e54"
                  },
                  "year": 2022,
                  "track_count": 28,
                  "month": 7,
                  "day": 8
                },
                {
                  "uri": "spotify:album:2KLsa5rYaZIgTI02b0oRyh",
                  "name": "Christmas Dinner 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f9247bf879598d0fae8d3033"
                  },
                  "year": 2021,
                  "track_count": 67,
                  "month": 12,
                  "day": 10
                },
                {
                  "uri": "spotify:album:7sKPWXzptrLG6Lt3HZXrds",
                  "name": "Mistletoe",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d406b051b0798269bfbb64b1"
                  },
                  "year": 2021,
                  "track_count": 37,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:7d1L92sNLG5FL2eCde6xE8",
                  "name": "Summer Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022afc1bdbf45340bc0e90c792"
                  },
                  "year": 2020,
                  "track_count": 24,
                  "month": 11,
                  "day": 6
                },
                {
                  "uri": "spotify:album:322Bo7RY7zxUrOSni3Mzql",
                  "name": "Weihnachtshits 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b7fe20cf018fbe31d92ef023"
                  },
                  "year": 2020,
                  "track_count": 24,
                  "month": 9,
                  "day": 5
                },
                {
                  "uri": "spotify:album:3jtJtFUKUq9e3GivW4HjtV",
                  "name": "Lounge + Chill Jazz , Blues, Funk Edition",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0254a054ef14c768683b9825c3"
                  },
                  "year": 2023,
                  "track_count": 55,
                  "month": 6,
                  "day": 9
                },
                {
                  "uri": "spotify:album:44vYrh7M2nzANauS9GAuoF",
                  "name": "Pop to Disconnect",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0212ba986af2f3e14c47755b5e"
                  },
                  "year": 2023,
                  "track_count": 25,
                  "month": 2,
                  "day": 22
                },
                {
                  "uri": "spotify:album:1NVsgEULGvnz55V9vwYqJU",
                  "name": "Rockin' Around The Christmas Tree",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ba4fbd69a3e50a4cd550e561"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:38WqcEoRUpx1srzSz7zjtF",
                  "name": "Vánoce 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f9c1a78eb1ee6100b35788f7"
                  },
                  "year": 2022,
                  "track_count": 39,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:4eTJ1nc0pl9UlTfTcIJoWp",
                  "name": "Holiday Hits - New Christmas Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022bf46c11e02d4712a27058f8"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:4vOcPm2ZpXvpL882GJPdap",
                  "name": "Ho Ho Hoy es Navidad",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e85844c8007c16582456fd29"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:7mun9r8Ozrh55Tj7tne4Y3",
                  "name": "All I Want for Christmas Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022cffebac79898c7117e922c9"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:6E5c4YVsq2GaSgWHmU1vGh",
                  "name": "Christmas Feels",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028157e11f96f02f142a28bf22"
                  },
                  "year": 2022,
                  "track_count": 32,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:1RerTWihRCmmBphTVg1SmQ",
                  "name": "Last Christmas Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fa2b430fd7a9637849fc9390"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:65zD7XiUxMYxlhk7B5N0qO",
                  "name": "Holiday Season 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f325607bfe3d278b4ff8cdb5"
                  },
                  "year": 2022,
                  "track_count": 58,
                  "month": 11,
                  "day": 28
                },
                {
                  "uri": "spotify:album:0mhPi0uPtExq6yE9CjtH5o",
                  "name": "Christmas Vibes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0295ad3f0d65677adce7a7cec0"
                  },
                  "year": 2022,
                  "track_count": 40,
                  "month": 11,
                  "day": 18
                },
                {
                  "uri": "spotify:album:0nfGhHhqFnOBvn35bdLcAb",
                  "name": "La magia del Natale",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0291d3dfac4348fb08f2410c7c"
                  },
                  "year": 2022,
                  "track_count": 26,
                  "month": 11,
                  "day": 18
                },
                {
                  "uri": "spotify:album:0nJYUunWntfhnPRnnLlCes",
                  "name": "Christmas Break!",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029df03082dd01b3743d417898"
                  },
                  "year": 2022,
                  "track_count": 54,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:77smMx7mIChlNYjZZZJiC5",
                  "name": "NATALE musica di festa",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a949a5174007ed30e407ce05"
                  },
                  "year": 2022,
                  "track_count": 53,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:1elM9otVitZPwvQtFems11",
                  "name": "brokenhearted at christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b586eaf87349678f0a23a854"
                  },
                  "year": 2022,
                  "track_count": 20,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:3toir2wKvsjBpvCsrCc3BP",
                  "name": "Kids Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e51833c621384791be3f6bf4"
                  },
                  "year": 2022,
                  "track_count": 41,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:77Hc45HFWTYPDIPVkmoBAT",
                  "name": "Weihnachtsparty",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02557208dd15f24dcf2c1401e6"
                  },
                  "year": 2022,
                  "track_count": 66,
                  "month": 11,
                  "day": 2
                },
                {
                  "uri": "spotify:album:0Jiwtlt64dcDOLFq8zlckV",
                  "name": "Country Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02108188e1959b3ef95adff307"
                  },
                  "year": 2022,
                  "track_count": 39,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:3Yilg019aEIgeemUZ2PGwq",
                  "name": "pov: you are santa",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bcfaba1d12c7da1244e478ab"
                  },
                  "year": 2022,
                  "track_count": 119,
                  "month": 10,
                  "day": 25
                },
                {
                  "uri": "spotify:album:68i2Xo8Ze09qJQvleeIalE",
                  "name": "Christmas Best Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fc9a8ab7b3ca3a393f801a09"
                  },
                  "year": 2022,
                  "track_count": 98,
                  "month": 10,
                  "day": 19
                },
                {
                  "uri": "spotify:album:6Zx95F2iCbj61cMOITE0ck",
                  "name": "Merry Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f1885d9bc5754b9e812e5d6e"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:6SVCpz3HrA6O2Nw2i4Vc9l",
                  "name": "Perfect Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b821ca68d09db9dfb354c73d"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:4N4LLC1eZkWBEdCEUtAP8P",
                  "name": "Christmas Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02acffd2b95bc570c71ad030d4"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:6hfqD6g2bhSQSckYlyZCMT",
                  "name": "Have Yourself a Merry Little Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025f48921c46f76788878ed000"
                  },
                  "year": 2022,
                  "track_count": 70,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:3fVQkpUthGiejS4ABXR0wP",
                  "name": "Happy Holidays",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e0fd78f85ff80395ffdbbdbc"
                  },
                  "year": 2022,
                  "track_count": 80,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:4wXdRhe0xBbFn35zgS24Nr",
                  "name": "Hits de Noël 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0255d554e3e4f0926feb78174c"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:5nlj3Afidio2y3JZjIkyhB",
                  "name": "Happy Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f5229f6856db1595713bcfb2"
                  },
                  "year": 2022,
                  "track_count": 80,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:3AmBldOm7KTLWBJKPfWMiX",
                  "name": "Christmas Party 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f6eba04607f9e7a9d1abc3a1"
                  },
                  "year": 2021,
                  "track_count": 24,
                  "month": 11,
                  "day": 22
                },
                {
                  "uri": "spotify:album:68fVTZVjYOIzZIsHB4pJ7N",
                  "name": "Chilled Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0204a6149942430f1bfae8a6b0"
                  },
                  "year": 2021,
                  "track_count": 30,
                  "month": 10,
                  "day": 27
                },
                {
                  "uri": "spotify:album:1icWjrP0u5YR8sS8kGZcpA",
                  "name": "Tis' The Season",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021c13deaadb1cb5aeb45351b6"
                  },
                  "year": 2021,
                  "track_count": 67,
                  "month": 10,
                  "day": 26
                },
                {
                  "uri": "spotify:album:36SHAooQxt8Qepgv4Lz8rY",
                  "name": "Classic Christmas Songs 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020bc7a925319c83fe04ad4f36"
                  },
                  "year": 2021,
                  "track_count": 27,
                  "month": 10,
                  "day": 22
                },
                {
                  "uri": "spotify:album:03bUtsW2uBqJWSe46pMUl1",
                  "name": "Un Crăciun modern",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02302547d6a473cdd668653860"
                  },
                  "year": 2021,
                  "track_count": 51,
                  "month": 10,
                  "day": 21
                },
                {
                  "uri": "spotify:album:4d0SzjDIptWE9gIdCdMNKN",
                  "name": "Julfavoriter 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0280490398c5d7e4e494829c6f"
                  },
                  "year": 2021,
                  "track_count": 113,
                  "month": 10,
                  "day": 15
                },
                {
                  "uri": "spotify:album:7COsYgGS5W27p90yEjfzBN",
                  "name": "Novogodišnja Večera",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0229c55c26badf7dee983a627a"
                  },
                  "year": 2020,
                  "track_count": 41,
                  "month": 12,
                  "day": 11
                },
                {
                  "uri": "spotify:album:2jM3hSRUb1U55ok8ad85tb",
                  "name": "Weihnachten 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0236b792d8ffa97d7596732108"
                  },
                  "year": 2020,
                  "track_count": 26,
                  "month": 9,
                  "day": 5
                },
                {
                  "uri": "spotify:album:5MFf2vztZlKFzrTNxG1UUS",
                  "name": "Weihnachtssongs 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02dba34a23940ab36b0309eb61"
                  },
                  "year": 2020,
                  "track_count": 26,
                  "month": 9,
                  "day": 5
                },
                {
                  "uri": "spotify:album:7rPAz3weSM6OYlD2WzTA08",
                  "name": "Joy To The World",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ad64bc03a87a6f0e900ba374"
                  },
                  "year": 2019,
                  "track_count": 29,
                  "month": 12,
                  "day": 20
                },
                {
                  "uri": "spotify:album:7etE8UcsRcuqiv2qaINj9J",
                  "name": "Christmas Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028403fa83e46095690f8f2e71"
                  },
                  "year": 2018,
                  "track_count": 55,
                  "month": 11,
                  "day": 30
                },
                {
                  "uri": "spotify:album:3hGgrERS3ILlWasMaD2Q92",
                  "name": "On the Radio - 00s",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029e3cc2ca447434ede72be34b"
                  },
                  "year": 2023,
                  "track_count": 60,
                  "month": 6,
                  "day": 10
                },
                {
                  "uri": "spotify:album:6dlTHTd1OyuXssQrPPkm2e",
                  "name": "Favoritas de Navidad",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02b2bc5ae7ecec55d2e6ad20a1"
                  },
                  "year": 2023,
                  "track_count": 30,
                  "month": 1,
                  "day": 20
                },
                {
                  "uri": "spotify:album:1ixtF5JhFcbmlPn4XA1ego",
                  "name": "Happy New Year Country Style",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0227b8d406fd24fc2d25a2820a"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 12,
                  "day": 30
                },
                {
                  "uri": "spotify:album:3YTNttaMA8WLqEdmz2hAeN",
                  "name": "Kerst Liedjes",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023c3b857dbb27b57b0c7bdee8"
                  },
                  "year": 2022,
                  "track_count": 49,
                  "month": 12,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3ZUeAUlBDJLY8pHZj9xwdO",
                  "name": "Complete Country Christmas - 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023ea74b4d97b9579537d5cc94"
                  },
                  "year": 2022,
                  "track_count": 42,
                  "month": 12,
                  "day": 9
                },
                {
                  "uri": "spotify:album:4oGyRtGLZespSePa1ZCT51",
                  "name": "It's Beginning to Look a Lot Like Christmas Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02497bedd80d45ecf506d88ea3"
                  },
                  "year": 2022,
                  "track_count": 25,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:57MPhuBwTZqDAglHXSIIxs",
                  "name": "Holly Jolly Christmas Hits 2022 (Merry Christmas)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021813901d9cc234090ee86ca8"
                  },
                  "year": 2022,
                  "track_count": 23,
                  "month": 12,
                  "day": 2
                },
                {
                  "uri": "spotify:album:4XEQ6h8EhFuJ6zeFWlUMRU",
                  "name": "Country for Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d3e20b675c7d323ced16927a"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:3E4qBNPqF8oqkXA2YNfUdu",
                  "name": "Holiday Jams",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0278cd84bd7e8507b64d76bc89"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 11,
                  "day": 25
                },
                {
                  "uri": "spotify:album:1Q6uvKGqYq1PBMfOchBkps",
                  "name": "The Ultimate Christmas Party Playlist 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0212331a1e42b64fe8f3b7fe73"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 11,
                  "day": 17
                },
                {
                  "uri": "spotify:album:5jbWW8oMr6UQFKP2IYQ1rH",
                  "name": "BUON NATALE 2023",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02a9c3e7b82dfb13f12f78a6ad"
                  },
                  "year": 2022,
                  "track_count": 61,
                  "month": 11,
                  "day": 16
                },
                {
                  "uri": "spotify:album:3JoQNSlrSByTz8KVzgOKvX",
                  "name": "Pranzo di Natale",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020570c5b93c413e3e0541650b"
                  },
                  "year": 2022,
                  "track_count": 43,
                  "month": 11,
                  "day": 15
                },
                {
                  "uri": "spotify:album:03vCF7FmhrchW2kRZVWDRT",
                  "name": "Irving Berlin - Greatest Seasonal Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0219f3885c8a77871ae723fd0c"
                  },
                  "year": 2022,
                  "track_count": 6,
                  "month": 11,
                  "day": 14
                },
                {
                  "uri": "spotify:album:6afFf12bomqxBPP27VKQhX",
                  "name": "Holiday Party 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02624b98d7c55a24db98c3d7a6"
                  },
                  "year": 2022,
                  "track_count": 57,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:0nhPJF8P9LUCFTzoNo0JoT",
                  "name": "Playlist di Natale",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0203726c06a3a6951ab68da917"
                  },
                  "year": 2022,
                  "track_count": 46,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:3jdOgsGCO9oztx1XMeEYMq",
                  "name": "X-Mas Party 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02125bf5c088b671fd3e5b1f41"
                  },
                  "year": 2022,
                  "track_count": 57,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:5EEfidbWdZNVnf8NptqvHJ",
                  "name": "Christmas Party 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d6e3b0ea81256396e6061471"
                  },
                  "year": 2022,
                  "track_count": 27,
                  "month": 11,
                  "day": 11
                },
                {
                  "uri": "spotify:album:4bcADs1EhQ4EZLaQrMuZAf",
                  "name": "Weihnachtsklassiker",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025a31b9f580cf84a6262c2c39"
                  },
                  "year": 2022,
                  "track_count": 58,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:4THN4ghSLOYyWFv8UdaCNv",
                  "name": "Noel jazz 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0227b8749dd91c418d639547f6"
                  },
                  "year": 2022,
                  "track_count": 43,
                  "month": 11,
                  "day": 4
                },
                {
                  "uri": "spotify:album:741Cl4JEFNMU3PaPz2GkIN",
                  "name": "pov: you have a holly jolly christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ea36099ab67cac2d885b272c"
                  },
                  "year": 2022,
                  "track_count": 74,
                  "month": 10,
                  "day": 20
                },
                {
                  "uri": "spotify:album:2ahbGlkMjCvhCgOujz8OP1",
                  "name": "All I Want For Christmas Is You",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023bfce57a5297ca6d866788f9"
                  },
                  "year": 2022,
                  "track_count": 97,
                  "month": 10,
                  "day": 18
                },
                {
                  "uri": "spotify:album:0j5x0wVB7MhvJZk1XODFa2",
                  "name": "Happy Country Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02354336ee5a9026f135995fa7"
                  },
                  "year": 2022,
                  "track_count": 45,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:6hbKWs6sKpHlm2h70YHu3D",
                  "name": "Country Christmas Time",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0227cddd6c4b3572a82912c401"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:4Tllv5UiItIHW2Y9vQVfjO",
                  "name": "Cuddle Up Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e027b1a8d5aa6f26cd506a8ff99"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:1m46i5aqfvPu0V8pouQ8PU",
                  "name": "Christmas Music - Holiday Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0213185dfd192e1aece8d9979d"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:1H3eGplpScENO7UrFPF52E",
                  "name": "Kerst Bij De Boom 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e024f158ac4ce8cc2ee482a0936"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 7
                },
                {
                  "uri": "spotify:album:3nUslDvApOojOZsitbqhFk",
                  "name": "Holiday Anthems 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0250b2294259396a273da80df6"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 5
                },
                {
                  "uri": "spotify:album:40hvDCUzJlxsBepOWmE1V6",
                  "name": "Christmas (Baby Please Come Home)",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025dde33e4c430fef653c7e91b"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4KLcKUXNi9BpwC4A7SmpF0",
                  "name": "Christmas Eve Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022d482a8e6eca19da23e30380"
                  },
                  "year": 2022,
                  "track_count": 75,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:1YY3MVi31ppkETArQXZuRc",
                  "name": "The Christmas Song - Chestnuts Roasting On an Open Fire",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021c1e31d33890d81ef440effd"
                  },
                  "year": 2022,
                  "track_count": 65,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:5XWaI12TVAjHP2VLjAgSIf",
                  "name": "Christmas 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0299034635740f3a289886a366"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:3HjLRAiUQPBfY3aoqCKOXK",
                  "name": "Frosty The Snowman",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02ce53135cee7fe4488149956a"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0kdIJZ7T44dlfXSp1EIvW8",
                  "name": "Ave Maria - Merry Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d1df4eb689c09dc19e3ec274"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2uCBhW26QKLJHqbcQNf6ps",
                  "name": "50 Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02990ce72f4671524741de64ff"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:5NQl38RBCEIhy66I9P2V2i",
                  "name": "Santa Claus Is Coming to Town",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e025d7544631a961954e11e9265"
                  },
                  "year": 2022,
                  "track_count": 65,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:1lpu8IuWPyAhzWbVqIQ57q",
                  "name": "I'll Be Home for Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02f6b76f0164787ef620bf9b52"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2SerTDVnNyzDPLiAgoZt5Y",
                  "name": "Santa, Can't You Hear Me",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e022d646d791a52233114a6443f"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4A2ygrBJpO5EJSUvwExYiR",
                  "name": "Jingle Bells - Happy Holidays",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021d5f2b046ef94246a7f2fac7"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4A2fDM4BBpIV605dFgdEzM",
                  "name": "All I Want for Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029e18666cd9ed66d4c434f26f"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:6g7D82WV6DFDDLW3ivFLgf",
                  "name": "Santa Baby - Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021dea5d743d66a568608454df"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:6oEihkYAJKb2dRqaNS42Ja",
                  "name": "Blue Christmas - Holiday Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d9bfec85d84382c465c6ad94"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:1XjAZlt4f3dOAq4s5jmarL",
                  "name": "Happy Xmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02caa9e08659c4ba997062fc37"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:3o91KoWyJSiVUNGLm8OwaJ",
                  "name": "Winter Wonderland",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0248bfe952192861dadc308a9a"
                  },
                  "year": 2022,
                  "track_count": 55,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2fVG7h9vncDktLG86LlnNE",
                  "name": "Christmas in America",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e021b1493873f6284c78018b8ba"
                  },
                  "year": 2022,
                  "track_count": 60,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:4d8N5F6dCP8AywWbIKVLz0",
                  "name": "White Christmas - Holiday Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0208b399bb8f0395f301b1fdbd"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:5PQqt6x5TuimXMqohzQWh2",
                  "name": "Christmas Country",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02abad57141d8fad9d1a383063"
                  },
                  "year": 2022,
                  "track_count": 35,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:2C0yRMifOJ7Q1sIpbkB70g",
                  "name": "Silver Bells - Christmas Holiday",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02cc870ea42738b8698a875751"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:0bjkdTvqLFlMKnbXb6EBCZ",
                  "name": "Silent Night - Christmas Crooners",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0276fc756747b14aaf58cbf60c"
                  },
                  "year": 2022,
                  "track_count": 30,
                  "month": 10,
                  "day": 1
                },
                {
                  "uri": "spotify:album:10TIdumPPqCDZ1bOLN2Seg",
                  "name": "Christmas",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0289cbf34e5a1ee8913d90f246"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:2RD5gx0Vibxfh8HA3TmweC",
                  "name": "Holiday Music",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026701541d6d75f2e70787347e"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:0qbmUilXeYtsHCtJVN3PQ1",
                  "name": "Holiday Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c660b2fe1441b9eb2f05f9de"
                  },
                  "year": 2022,
                  "track_count": 50,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:7KU6mMQLeFzfSzbgxR9jSB",
                  "name": "Easy Christmas Hits",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0225b532b75235c56c1b236e67"
                  },
                  "year": 2022,
                  "track_count": 24,
                  "month": 9,
                  "day": 30
                },
                {
                  "uri": "spotify:album:4wLSYZyUs7E6B9gGh5hiRA",
                  "name": "Home for Christmas - Country Holiday",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02334b3dfab56296d272f48cd2"
                  },
                  "year": 2022,
                  "track_count": 32,
                  "month": 9,
                  "day": 16
                },
                {
                  "uri": "spotify:album:6jbGw9gGheI9Qn5VN0j1oC",
                  "name": "Holiday 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e026c29ed17be27203524487355"
                  },
                  "year": 2021,
                  "track_count": 35,
                  "month": 12,
                  "day": 14
                },
                {
                  "uri": "spotify:album:5OMD4cWXc2M2TFyahe5Tvr",
                  "name": "Holiday Hits 2022",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e028a9054b33360ae48cd580895"
                  },
                  "year": 2021,
                  "track_count": 34,
                  "month": 12,
                  "day": 14
                },
                {
                  "uri": "spotify:album:5jjQaiqSBfeajIVB8ndt1J",
                  "name": "Božićna Večera",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0289b610293df23acf179afdca"
                  },
                  "year": 2021,
                  "track_count": 35,
                  "month": 12,
                  "day": 7
                },
                {
                  "uri": "spotify:album:4YQUAOXTY5b4zRnMHtWN6N",
                  "name": "Merry Christmas Eve 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e020a214d958636e8252f851e21"
                  },
                  "year": 2021,
                  "track_count": 38,
                  "month": 11,
                  "day": 12
                },
                {
                  "uri": "spotify:album:3wAjwD9Qrimd5nu3wygqfr",
                  "name": "Božična Večerja",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023e6fae6dfad5e7f260fcae59"
                  },
                  "year": 2021,
                  "track_count": 34,
                  "month": 11,
                  "day": 12
                },
                {
                  "uri": "spotify:album:6mTVmfDRGlWm4Hy85iSzlT",
                  "name": "Christmas All Over",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02d34220238cb2082afe90fb52"
                  },
                  "year": 2021,
                  "track_count": 30,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:5EoLysMZEBShqCd3bGkPJS",
                  "name": "Christmas Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02fff46f7eebc063ae7c8f9ade"
                  },
                  "year": 2021,
                  "track_count": 26,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:5QLkKDShck8OVVxrV3eHBE",
                  "name": "Candlelight Christmas Dinner",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02c031e10775730b1bbbe8fa1a"
                  },
                  "year": 2021,
                  "track_count": 27,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:6UVqLrLpNJiVUqlpu6yntf",
                  "name": "Cozy Holidays 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e023e21a7b1b7f1d55511302986"
                  },
                  "year": 2021,
                  "track_count": 23,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:0w9sE9H3WSyP81FEYy2cRf",
                  "name": "Festive Christmas Classics",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0246a17d34450180c8c8031c7a"
                  },
                  "year": 2021,
                  "track_count": 28,
                  "month": 10,
                  "day": 28
                },
                {
                  "uri": "spotify:album:5DXemtL8LT9eAwSlrMDvXf",
                  "name": "Best of Christmas Songs",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0249065af828f69193b28890bb"
                  },
                  "year": 2021,
                  "track_count": 23,
                  "month": 10,
                  "day": 22
                },
                {
                  "uri": "spotify:album:1198Mvr1OrvFkpy2V4O4li",
                  "name": "Christmas 2020",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02467b23f8a9fb9770872eacee"
                  },
                  "year": 2021,
                  "track_count": 19,
                  "month": 10,
                  "day": 22
                },
                {
                  "uri": "spotify:album:1cIDFUScHUSMxqU7xMi4iL",
                  "name": "Jingle Bell Rock",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02bb2e3809d800566519228a1b"
                  },
                  "year": 2021,
                  "track_count": 25,
                  "month": 10,
                  "day": 14
                },
                {
                  "uri": "spotify:album:5nAd5lFawE6vDmUgKgDo2G",
                  "name": "Happy Holidays 2021",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e02e74fc42771a090573ccca4ac"
                  },
                  "year": 2021,
                  "track_count": 67,
                  "month": 10,
                  "day": 12
                },
                {
                  "uri": "spotify:album:3UxjjCARHrVRVRPQPyBBXy",
                  "name": "Julpynt",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e029d1ea02e3516b943f65d3ed2"
                  },
                  "year": 2021,
                  "track_count": 136,
                  "month": 10,
                  "day": 8
                },
                {
                  "uri": "spotify:album:73mQYxYyjIykc3nyrNbbNJ",
                  "name": "We All Love Ella: Celebrating The First Lady Of Song",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0286a34513f13e0fa11b088f13"
                  },
                  "year": 2007,
                  "track_count": 16,
                  "month": 1,
                  "day": 1
                }
              ],
              "total_count": 443
            },
            "compilations": {
              "releases": [
                {
                  "uri": "spotify:album:40Yyw4dYduC7YzFzbXud5Q",
                  "name": "Hollywood The Deluxe EP",
                  "cover": {
                    "uri": "https://i.scdn.co/image/ab67616d00001e0257968f2d1fd0dbb7c9abcaf6"
                  },
                  "year": 2009,
                  "track_count": 8,
                  "month": 10,
                  "day": 9
                }
              ],
              "total_count": 1
            }
          },
          "merch": {
            "items": [
              {
                "name": "Call Me Irresponsible Vinyl Record",
                "description": "Protection Each record is protected within its record sleeve by a white vellum anti-dust sleeve. Packaging All items are shipped brand-new and unopened in original packaging. Every record is shipped in original factory-applied shrink wrap and has never been touched by human hands.",
                "link": "https://www.merchbar.com/pop/michael-bublé/michael-buble-call-me-irresponsible-vinyl-record?utm_source=merchbar-spotify&utm_medium=affiliate&mb-listing-id=471080v2464&shi=008c6e168e83c98dd74011c03555a5190050e4214086864525170a",
                "image_uri": "https://merch-img.scdn.co/https%3A%2F%2Fmerchbar.imgix.net%2Fproduct%2Fvinylized%2Fupc%2F81%2F093624998112.jpg%3Fblend64%3DaHR0cHM6Ly9tZXJjaGJhci5pbWdpeC5uZXQvfnRleHQ_dHh0LWZvbnQ9c2Fucy1zZXJpZi1ib2xkJnR4dC1jb2xvcj1mZmYmdHh0LXNpemU9NjQmdHh0LXBhZD0xNiZ3PTMyMCZiZz1mNzMxMTkmZHByPTImdHh0LWFsaWduPW1pZGRsZSUyQ2NlbnRlciZ0eHQ2ND1UMDRnVTBGTVJRJTNEJTNE%26blend-mode%3Dnormal%26blend-align%3Dbottom%2Cleft%26dpr%3D2%26blend-w%3D0.75%26w%3D640%26h%3D640?h=300&w=300&s=48b388f2ad25594a93789d5403cbe4f6",
                "price": "$31.49",
                "uuid": "471080"
              },
              {
                "name": "It's Time Vinyl Record",
                "description": "Protection Each record is protected within its record sleeve by a white vellum anti-dust sleeve. Packaging All items are shipped brand-new and unopened in original packaging. Every record is shipped in original factory-applied shrink wrap and has never been touched by human hands.",
                "link": "https://www.merchbar.com/pop/michael-bublé/michael-buble-it-s-time-vinyl-record?utm_source=merchbar-spotify&utm_medium=affiliate&mb-listing-id=471663v2464&shi=008c6e168e83c98dd74011c03555a5190050e4214086864525170a",
                "image_uri": "https://merch-img.scdn.co/https%3A%2F%2Fmerchbar.imgix.net%2Fproduct%2F4%2F1616%2F33777511%2F093624924043.jpg%3Fblend64%3DaHR0cHM6Ly9tZXJjaGJhci5pbWdpeC5uZXQvfnRleHQ_dHh0LWZvbnQ9c2Fucy1zZXJpZi1ib2xkJnR4dC1jb2xvcj1mZmYmdHh0LXNpemU9NjQmdHh0LXBhZD0xNiZ3PTMyMCZiZz1mNzMxMTkmZHByPTImdHh0LWFsaWduPW1pZGRsZSUyQ2NlbnRlciZ0eHQ2ND1UMDRnVTBGTVJRJTNEJTNE%26blend-mode%3Dnormal%26blend-align%3Dbottom%2Cleft%26dpr%3D2%26blend-w%3D0.75%26w%3D640%26h%3D640?h=300&w=300&s=a606d78d22701f09414af2d481f6d158",
                "price": "$31.49",
                "uuid": "471663"
              },
              {
                "name": "Michael Buble LP - Christmas",
                "description": "Christmas ArtistMichael BublÌ© Performer The Puppini Sisters,Shania Twain,Thalia Producer Bob Rock Format:Vinyl / 12\" Album Label:Warner Bros Records Catalogue No:0093624934998 Barcode:0093624934998 Genre:Pop No of Discs:1 Release Date:20 Oct 2014 Running Time:51:44 minutes Weight:314g Dimensions:125 x 4 x 110(mm) Track Listings Disc 1 1It's Beginning to Look a Lot Like Christmas 2Santa Claus Is Coming to Town 3Jingle Bells (Feat. The Puppini Sisters) 4White Christmas 5All I Want for Christmas Is You 6Have a Holly Jolly Christmas 7Santa Baby 8Have Yourself a Merry Little Christmas 9Christmas (Baby Please Come Home) 10Silent Night 11Blue Christmas 12Cold December Night 13I'll Be Home for Christmas 14Ave Maria 15Mis Deseos/Feliz Navidad",
                "link": "https://www.merchbar.com/pop/michael-bublé/michael-bubl-lp-christmas-vinyl-vinyl?utm_source=merchbar-spotify&utm_medium=affiliate&mb-listing-id=2289842v2464&shi=008c6e168e83c98dd74011c03555a5190050e4214086864525170a",
                "image_uri": "https://merch-img.scdn.co/https%3A%2F%2Fmerchbar.imgix.net%2Fproduct%2F194%2F7952%2F7541436154114%2FAKAi-9U-0093624934998.jpg%3Fdpr%3D2%26w%3D640%26h%3D640?h=300&w=300&s=169f29725e50317ff2a9804080f8f6ee",
                "price": "$47.80",
                "uuid": "2289842"
              }
            ]
          },
          "gallery": {
            "images": [
              {
                "uri": "https://i.scdn.co/image/ab6772690000bac3868fa798866c8752c179132d"
              },
              {
                "uri": "https://i.scdn.co/image/99e17026d863df23982b259c54216d137de2086b"
              },
              {
                "uri": "https://i.scdn.co/image/992cfd41d020bcafc93700957ad7ef0654379834"
              },
              {
                "uri": "https://i.scdn.co/image/c3dd3832e2180f8b23d8a88d573a734f66a04411"
              },
              {
                "uri": "https://i.scdn.co/image/e8695e042b918dae9e07c2a471a49ef5a1f911c4"
              },
              {
                "uri": "https://i.scdn.co/image/d53efab52f9e89611ef589212e30cb8d5869d83f"
              },
              {
                "uri": "https://i.scdn.co/image/44c165967565cb9a93bb36677f607d7b07e83a1f"
              },
              {
                "uri": "https://i.scdn.co/image/6e80e51cc261790d3d24da0110ce74ff5951d59e"
              },
              {
                "uri": "https://i.scdn.co/image/18ed997903fc68b151a36c2734cfd3c1b07de3dd"
              },
              {
                "uri": "https://i.scdn.co/image/50a3fcf375d1208d748da7a8bc13d74d1aa26064"
              },
              {
                "uri": "https://i.scdn.co/image/e5f750d1d6e551c7a6f81d96b76e2a19ce09d853"
              },
              {
                "uri": "https://i.scdn.co/image/3957dac9116368a1b5194b2059ca8a5eda3fe49f"
              },
              {
                "uri": "https://i.scdn.co/image/8e3c29a800218f80848afbd6c8b6c0f2650c3d69"
              },
              {
                "uri": "https://i.scdn.co/image/75578ea4f78718cf9aede291232081bf7644a6ab"
              },
              {
                "uri": "https://i.scdn.co/image/919a1ee770549b2ba1d518007c906ef5ecf16927"
              },
              {
                "uri": "https://i.scdn.co/image/729dd09ce124868c239aa92ce450b0ece1f8eb15"
              },
              {
                "uri": "https://i.scdn.co/image/071be0bf380239fbde4d2bf68458ec73119f2124"
              },
              {
                "uri": "https://i.scdn.co/image/01ebf742d4a62d1a2c258c9ba0f8eb987f6c8b1e"
              },
              {
                "uri": "https://i.scdn.co/image/67e1a2299cc21626f23e554d147847f118780243"
              },
              {
                "uri": "https://i.scdn.co/image/800742b11f5b183e9604cae0f9209761611cdf62"
              },
              {
                "uri": "https://i.scdn.co/image/c612d336ade8c226a01218d6f4631b85fb15b652"
              },
              {
                "uri": "https://i.scdn.co/image/f561e7cd57f89bd3bf5b2c197e0421ed7a8adeb0"
              }
            ]
          },
          "published_playlists": {
            "playlists": [
              {
                "uri": "spotify:playlist:4F1LJJA4a5qEZdtK3rczD2",
                "name": "Michael Bublé: Sway Higher",
                "cover": {
                  "uri": "https://i.scdn.co/image/ab67706c0000da841d6f3ae177131c1f49cc80ea"
                },
                "follower_count": 179065
              },
              {
                "uri": "spotify:playlist:3pEEs6bJsz3xQQfcpzEZzW",
                "name": "Michael Bublé Discographé",
                "cover": {
                  "uri": "https://i.scdn.co/image/ab67706c0000da847323a7e30270571074d02635"
                },
                "follower_count": 50430
              },
              {
                "uri": "spotify:playlist:78zxE2XW5bh1qggCJOigeB",
                "name": "Michael Bublé - Higher Tour Setlist 2023",
                "cover": {
                  "uri": "https://i.scdn.co/image/ab67706c0000da848597547c7fdd0935f1ba5389"
                },
                "follower_count": 265
              },
              {
                "uri": "spotify:playlist:4R58ZHgiSrORyJH04n0QJ3",
                "name": "Michael Bublé · Tour Stop · 148",
                "cover": {
                  "uri": "https://i.scdn.co/image/ab67706c0000da84a9f26e83cdf8d4ee84705568"
                },
                "follower_count": 2907
              },
              {
                "uri": "spotify:playlist:2m3lenloabKmseEPp2uLDk",
                "name": "Michael Bublé - #NowPlaying",
                "cover": {
                  "uri": "https://mosaic.scdn.co/300/ab67616d00001e023b11178cccd78ec77fc12dbcab67616d00001e0262edffdb4d8b52cf89f39db6ab67616d00001e02ccd9af18cc83991382c9ab9aab67616d00001e02ef37416970812293c08e8a78"
                },
                "follower_count": 6371
              }
            ]
          },
          "monthly_listeners": { "listener_count": 11674223 },
          "creator_about": { "monthlyListeners": 11674223 }
        }
        
        """;
}