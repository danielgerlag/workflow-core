using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WorkflowCore.Persistence.SqlServer;

namespace WorkflowCore.Persistence.SqlServer.Migrations
{
    [DbContext(typeof(SqlServerPersistenceProvider))]
    [Migration("20161121195301_InitialDatabase")]
    partial class InitialDatabase
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedPublication", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.HasAnnotation("SqlServer:Schema", "wfc");

                    b.HasAnnotation("SqlServer:TableName", "UnpublishedEvent");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedSubscription", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.HasAnnotation("SqlServer:Schema", "wfc");

                    b.HasAnnotation("SqlServer:TableName", "Subscription");
                });

            modelBuilder.Entity("WorkflowCore.Persistence.EntityFramework.Models.PersistedWorkflow", b =>
                {
                    b.Property<long>("ClusterKey")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Data");

                    b.Property<string>("Description")
                        .HasMaxLength(500);

                    b.Property<string>("ExecutionPointers");

                    b.Property<Guid>("InstanceId")
                        .HasMaxLength(200);

                    b.Property<long?>("NextExecution");

                    b.Property<int>("Version");

                    b.Property<string>("WorkflowDefinitionId")
                        .HasMaxLength(200);

                    b.HasKey("ClusterKey");

                    b.HasIndex("InstanceId")
                        .IsUnique();

                    b.HasIndex("NextExecution");

                    b.ToTable("PersistedWorkflow");

                    b.HasAnnotation("SqlServer:Schema", "wfc");

                    b.HasAnnotation("SqlServer:TableName", "Workflow");
                });
        }
    }
}
