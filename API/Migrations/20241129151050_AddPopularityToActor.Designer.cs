﻿// <auto-generated />
using System;
using DeathflixAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DeathflixAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20241129151050_AddPopularityToActor")]
    partial class AddPopularityToActor
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DeathflixAPI.Models.Actor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Biography")
                        .HasColumnType("text");

                    b.Property<DateTime?>("DateOfBirth")
                        .HasColumnType("date");

                    b.Property<DateTime?>("DateOfDeath")
                        .HasColumnType("date");

                    b.Property<DateTime>("LastDeathCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastDetailsCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("PlaceOfBirth")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<double>("Popularity")
                        .HasColumnType("decimal(10,3)");

                    b.Property<string>("ProfileImagePath")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("TmdbId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Actors");
                });

            modelBuilder.Entity("DeathflixAPI.Models.DeathRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ActorId")
                        .HasColumnType("integer");

                    b.Property<string>("AdditionalDetails")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<string>("CauseOfDeath")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("DateOfDeath")
                        .HasColumnType("date");

                    b.Property<DateTime>("LastVerified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("PlaceOfDeath")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("SourceUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("Id");

                    b.HasIndex("ActorId")
                        .IsUnique();

                    b.ToTable("DeathRecords");
                });

            modelBuilder.Entity("DeathflixAPI.Models.Movie", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Overview")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("PosterPath")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime?>("ReleaseDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("TmdbId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Movies");
                });

            modelBuilder.Entity("DeathflixAPI.Models.MovieCredit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ActorId")
                        .HasColumnType("integer");

                    b.Property<string>("Character")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Department")
                        .HasColumnType("text");

                    b.Property<int>("MovieId")
                        .HasColumnType("integer");

                    b.Property<string>("Role")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.HasIndex("ActorId");

                    b.HasIndex("MovieId");

                    b.ToTable("MovieCredits");
                });

            modelBuilder.Entity("DeathflixAPI.Models.DeathRecord", b =>
                {
                    b.HasOne("DeathflixAPI.Models.Actor", "Actor")
                        .WithOne("DeathRecord")
                        .HasForeignKey("DeathflixAPI.Models.DeathRecord", "ActorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Actor");
                });

            modelBuilder.Entity("DeathflixAPI.Models.MovieCredit", b =>
                {
                    b.HasOne("DeathflixAPI.Models.Actor", "Actor")
                        .WithMany("MovieCredits")
                        .HasForeignKey("ActorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DeathflixAPI.Models.Movie", "Movie")
                        .WithMany("Credits")
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Actor");

                    b.Navigation("Movie");
                });

            modelBuilder.Entity("DeathflixAPI.Models.Actor", b =>
                {
                    b.Navigation("DeathRecord");

                    b.Navigation("MovieCredits");
                });

            modelBuilder.Entity("DeathflixAPI.Models.Movie", b =>
                {
                    b.Navigation("Credits");
                });
#pragma warning restore 612, 618
        }
    }
}
