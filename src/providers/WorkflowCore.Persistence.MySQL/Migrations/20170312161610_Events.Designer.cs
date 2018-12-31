using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkflowCore.Persistence.MySQL;
using WorkflowCore.Models;

namespace WorkflowCore.Persistence.MySQL.Migrations
{
    [DbContext(typeof(MysqlContext))]
    [Migration("20170312161610_Events")]
    partial class Events
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-rtm-30799")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedEvent", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("EventData");

                    b.Property<Guid>("EventId");

                    b.Property<string>("EventKey")
                        .HasMaxLength(200);

                    b.Property<string>("EventName")
                        .HasMaxLength(200);

                    b.Property<DateTime>("EventTime");

                    b.Property<bool>("IsProcessed");

                    b.HasKey("PersistenceId");

                    b.HasIndex("EventId")
                        .IsUnique();

                    b.HasIndex("EventTime");

                    b.HasIndex("IsProcessed");

                    b.HasIndex("EventName", "EventKey");

                    b.ToTable("PersistedEvent");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionError", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("ErrorTime");

                    b.Property<long>("ExecutionPointerId");

                    b.Property<string>("Id")
                        .HasMaxLength(50);

                    b.Property<string>("Message");

                    b.HasKey("PersistenceId");

                    b.HasIndex("ExecutionPointerId");

                    b.ToTable("PersistedExecutionError");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExecutionPointer", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

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
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedExtensionAttribute", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AttributeKey")
                        .HasMaxLength(100);

                    b.Property<string>("AttributeValue");

                    b.Property<long>("ExecutionPointerId");

                    b.HasKey("PersistenceId");

                    b.HasIndex("ExecutionPointerId");

                    b.ToTable("PersistedExtensionAttribute");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedSubscription", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("EventKey")
                        .HasMaxLength(200);

                    b.Property<string>("EventName")
                        .HasMaxLength(200);

                    b.Property<int>("StepId");

                    b.Property<DateTime>("SubscribeAsOf");

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
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedWorkflow", b =>
                {
                    b.Property<long>("PersistenceId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

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
