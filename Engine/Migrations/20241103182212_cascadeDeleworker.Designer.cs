﻿// <auto-generated />
using System;
using Engine.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Engine.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241103182212_cascadeDeleworker")]
    partial class cascadeDeleworker
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0-rc.2.24474.1");

            modelBuilder.Entity("Engine.DAL.Entities.EngineEntities", b =>
                {
                    b.Property<Guid>("EngineId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("InstallDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("EngineId");

                    b.ToTable("EngineEntities");
                });

            modelBuilder.Entity("Engine.DAL.Entities.HubUrlEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("EngineEntitiesEngineId")
                        .HasColumnType("TEXT");

                    b.Property<string>("HubUrl")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EngineEntitiesEngineId");

                    b.HasIndex("HubUrl")
                        .IsUnique();

                    b.ToTable("HubUrlEntity");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEntity", b =>
                {
                    b.Property<string>("WorkerId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Command")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("ImgWatchdogEnabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(true);

                    b.Property<TimeSpan>("ImgWatchdogGraceTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new TimeSpan(0, 0, 0, 10, 0));

                    b.Property<TimeSpan>("ImgWatchdogInterval")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new TimeSpan(0, 0, 0, 5, 0));

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("WatchdogEventCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("WorkerId");

                    b.ToTable("Workers");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEvent", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EventTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("WorkerId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("EventId");

                    b.HasIndex("WorkerId");

                    b.ToTable("WorkerEvents");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEventLog", b =>
                {
                    b.Property<int>("LogId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LogLevel")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LogTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("WorkerEventEventId")
                        .HasColumnType("INTEGER");

                    b.HasKey("LogId");

                    b.HasIndex("WorkerEventEventId");

                    b.ToTable("WorkerEventLog");
                });

            modelBuilder.Entity("Engine.DAL.Entities.HubUrlEntity", b =>
                {
                    b.HasOne("Engine.DAL.Entities.EngineEntities", null)
                        .WithMany("HubUrls")
                        .HasForeignKey("EngineEntitiesEngineId");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEvent", b =>
                {
                    b.HasOne("Engine.DAL.Entities.WorkerEntity", null)
                        .WithMany("Events")
                        .HasForeignKey("WorkerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEventLog", b =>
                {
                    b.HasOne("Engine.DAL.Entities.WorkerEvent", null)
                        .WithMany("EventLogs")
                        .HasForeignKey("WorkerEventEventId");
                });

            modelBuilder.Entity("Engine.DAL.Entities.EngineEntities", b =>
                {
                    b.Navigation("HubUrls");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEntity", b =>
                {
                    b.Navigation("Events");
                });

            modelBuilder.Entity("Engine.DAL.Entities.WorkerEvent", b =>
                {
                    b.Navigation("EventLogs");
                });
#pragma warning restore 612, 618
        }
    }
}
