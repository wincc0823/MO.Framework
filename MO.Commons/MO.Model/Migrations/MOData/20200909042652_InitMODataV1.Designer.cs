﻿// <auto-generated />
using MO.Model.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MO.Model.Migrations.MOData
{
    [DbContext(typeof(MODataContext))]
    [Migration("20200909042652_InitMODataV1")]
    partial class InitMODataV1
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("MO.Model.Entitys.GameUser", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<string>("HeadIcon")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<int>("NickName")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("GameUser");
                });
#pragma warning restore 612, 618
        }
    }
}
