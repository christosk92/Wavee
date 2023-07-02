﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wavee.Sqlite;

#nullable disable

namespace Wavee.Sqlite.Migrations
{
    [DbContext(typeof(LocalDbContext))]
    partial class LocalDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.8");

            modelBuilder.Entity("Wavee.Sqlite.Entities.CachedPlaylist", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("ImageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Playlists");
                });

            modelBuilder.Entity("Wavee.Sqlite.Entities.CachedPlaylistTrack", b =>
                {
                    b.Property<string>("PlaylistIdTrackIdCompositeKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("CachedPlaylistId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MetadataJson")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TrackId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Uid")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("PlaylistIdTrackIdCompositeKey");

                    b.HasIndex("CachedPlaylistId");

                    b.HasIndex("TrackId");

                    b.ToTable("PlaylistTracks");
                });

            modelBuilder.Entity("Wavee.Sqlite.Entities.CachedTrack", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("AlbumDiscNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AlbumName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("AlbumTrackNumber")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("CacheExpiration")
                        .HasColumnType("TEXT");

                    b.Property<int>("Duration")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LargeImageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MainArtistName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MediumImageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("OriginalData")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<string>("SmallImageId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TagsCommaSeparated")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tracks");
                });

            modelBuilder.Entity("Wavee.Sqlite.Entities.RawEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<DateTimeOffset>("Expiration")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("RawEntities");
                });

            modelBuilder.Entity("Wavee.Sqlite.Entities.CachedPlaylistTrack", b =>
                {
                    b.HasOne("Wavee.Sqlite.Entities.CachedPlaylist", null)
                        .WithMany("PlaylistTracks")
                        .HasForeignKey("CachedPlaylistId");

                    b.HasOne("Wavee.Sqlite.Entities.CachedTrack", "Track")
                        .WithMany()
                        .HasForeignKey("TrackId");

                    b.Navigation("Track");
                });

            modelBuilder.Entity("Wavee.Sqlite.Entities.CachedPlaylist", b =>
                {
                    b.Navigation("PlaylistTracks");
                });
#pragma warning restore 612, 618
        }
    }
}
