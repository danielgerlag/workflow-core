using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    [DbContext(typeof(PostgresPersistenceProvider))]
    [Migration("20161122173050_InitialDatabase")]
    partial class InitialDatabase
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedPublication", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("EventData");

                    b.Property<string>("EventKey")
                        .HasMaxLength(200);

                    b.Property<string>("EventName")
                        .HasMaxLength(200);

                    b.Property<Guid>("PublicationId");

                    b.Property<int>("StepId");

                    b.Property<string>("WorkflowId")
                        .HasMaxLength(200);

                    b.HasKey("ClusterKey");

                    b.HasIndex("PublicationId")
                        .IsUnique();

                    b.ToTable("PersistedPublication");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "UnpublishedEvent");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedSubscription", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("EventKey")
                        .HasMaxLength(200);

                    b.Property<string>("EventName")
                        .HasMaxLength(200);

                    b.Property<int>("StepId");

                    b.Property<Guid>("SubscriptionId")
                        .HasMaxLength(200);

                    b.Property<string>("WorkflowId")
                        .HasMaxLength(200);

                    b.HasKey("ClusterKey");

                    b.HasIndex("EventKey");

                    b.HasIndex("EventName");

                    b.HasIndex("SubscriptionId")
                        .IsUnique();

                    b.ToTable("PersistedSubscription");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "Subscription");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedWorkflow", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Data");

                    b.Property<string>("Description")
                        .HasMaxLength(500);

                    b.Property<string>("ExecutionPointers");

                    b.Property<Guid>("InstanceId")
                        .HasMaxLength(200);

                    b.Property<long?>("NextExecution");

                    b.Property<int>("Status");

                    b.Property<int>("Version");

                    b.Property<string>("WorkflowDefinitionId")
                        .HasMaxLength(200);

                    b.HasKey("ClusterKey");

                    b.HasIndex("InstanceId")
                        .IsUnique();

                    b.HasIndex("NextExecution");

                    b.ToTable("PersistedWorkflow");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "Workflow");
                });
        }
    }
}
