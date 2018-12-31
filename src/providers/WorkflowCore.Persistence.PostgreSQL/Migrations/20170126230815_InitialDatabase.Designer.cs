using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.PostgreSQL.Migrations
{
    [DbContext(typeof(PostgresContext))]
    [Migration("20170126230815_InitialDatabase")]
    partial class InitialDatabase
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionError", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ErrorTime");

                    b.Property<long>("ExecutionPointerId");

                    b.Property<string>("Id")
                        .HasMaxLength(50);

                    b.Property<string>("Message");

                    b.HasKey("PersistenceId");

                    b.HasIndex("ExecutionPointerId");

                    b.ToTable("PersistedExecutionError");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "ExecutionError");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionPointer", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("Active");

                    b.Property<int>("ConcurrentFork");

                    b.Property<DateTime?>("EndTime");

                    b.Property<string>("EventData");

                    b.Property<string>("EventKey");

                    b.Property<string>("EventName");

                    b.Property<bool>("EventPublished");

                    b.Property<string>("Id")
                        .HasMaxLength(50);

                    b.Property<bool>("PathTerminator");

                    b.Property<string>("PersistenceData");

                    b.Property<DateTime?>("SleepUntil");

                    b.Property<DateTime?>("StartTime");

                    b.Property<int>("StepId");

                    b.Property<string>("StepName");

                    b.Property<long>("WorkflowId");

                    b.HasKey("PersistenceId");

                    b.HasIndex("WorkflowId");

                    b.ToTable("PersistedExecutionPointer");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "ExecutionPointer");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExtensionAttribute", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AttributeKey")
                        .HasMaxLength(100);

                    b.Property<string>("AttributeValue");

                    b.Property<long>("ExecutionPointerId");

                    b.HasKey("PersistenceId");

                    b.HasIndex("ExecutionPointerId");

                    b.ToTable("PersistedExtensionAttribute");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "ExtensionAttribute");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedPublication", b =>
                {
                    b.Property<long>("PersistenceId")
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

                    b.HasKey("PersistenceId");

                    b.HasIndex("PublicationId")
                        .IsUnique();

                    b.ToTable("PersistedPublication");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "UnpublishedEvent");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedSubscription", b =>
                {
                    b.Property<long>("PersistenceId")
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

                    b.HasKey("PersistenceId");

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
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("CompleteTime");

                    b.Property<DateTime>("CreateTime");

                    b.Property<string>("Data");

                    b.Property<string>("Description")
                        .HasMaxLength(500);

                    b.Property<Guid>("InstanceId")
                        .HasMaxLength(200);

                    b.Property<long?>("NextExecution");

                    b.Property<int>("Status");

                    b.Property<int>("Version");

                    b.Property<string>("WorkflowDefinitionId")
                        .HasMaxLength(200);

                    b.HasKey("PersistenceId");

                    b.HasIndex("InstanceId")
                        .IsUnique();

                    b.HasIndex("NextExecution");

                    b.ToTable("PersistedWorkflow");

                    b.HasAnnotation("Npgsql:Schema", "wfc");

                    b.HasAnnotation("Npgsql:TableName", "Workflow");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionError", b =>
                {
                    b.HasOne("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionPointer", "ExecutionPointer")
                        .WithMany("Errors")
                        .HasForeignKey("ExecutionPointerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionPointer", b =>
                {
                    b.HasOne("WorkflowCore.Persistence.EntityFramework.Models.PersistedWorkflow", "Workflow")
                        .WithMany("ExecutionPointers")
                        .HasForeignKey("WorkflowId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExtensionAttribute", b =>
                {
                    b.HasOne("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionPointer", "ExecutionPointer")
                        .WithMany("ExtensionAttributes")
                        .HasForeignKey("ExecutionPointerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
