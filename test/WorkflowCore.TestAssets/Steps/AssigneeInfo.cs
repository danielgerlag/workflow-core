using System;
using System.Linq;

namespace WorkflowCore.TestAssets.Steps;

public class AssigneeInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MemberType { get; set; }

    public UnitInfo UnitInfo { get; set; }
}

public class UnitInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int UnitType { get; set; }
}