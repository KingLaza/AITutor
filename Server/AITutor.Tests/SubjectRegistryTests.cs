using AITutor.Core.Subjects;
using AITutor.Core.Subjects.OperatingSystems;
using AITutor.Core.Subjects.ProgramskiPrevodioci;

namespace AITutor.Tests;

public class SubjectRegistryTests
{
    private static SubjectRegistry Registry(string? defaultName = null) => new(new ISubjectProfile[]
    {
        new ProgramskiPrevodiociProfile(),
        new OperatingSystemsProfile(new FakeChat()),
    }, defaultName);

    [Fact]
    public void Lookup_is_case_insensitive() =>
        Assert.Equal("Operativni sistemi", Registry().Get("operativni SISTEMI").Name);

    [Fact]
    public void Default_is_the_first_registered_unless_configured()
    {
        Assert.Equal("Programski prevodioci", Registry().DefaultName);
        Assert.Equal("Operativni sistemi", Registry("Operativni sistemi").DefaultName);
    }

    [Fact]
    public void Unknown_subject_gets_a_generic_profile_without_extras()
    {
        var profile = Registry().Get("Neki novi predmet");

        Assert.Equal("Neki novi predmet", profile.Name);
        Assert.Null(profile.Solver);
        Assert.Null(profile.GetGuidance("Napisati monitor"));
        Assert.Empty(profile.ClassifierExamples);
    }

    [Fact]
    public void Each_subject_carries_its_own_threshold_and_extras()
    {
        var pp = Registry().Get("Programski prevodioci");
        var os = Registry().Get("Operativni sistemi");

        Assert.Equal(0.56, pp.MaxDistance);
        Assert.Equal(0.64, os.MaxDistance);
        Assert.Null(pp.Solver);
        Assert.NotNull(os.Solver);
        Assert.Null(pp.GetGuidance("Napisati monitor za sinhronizaciju"));
        Assert.NotNull(os.GetGuidance("Napisati monitor za sinhronizaciju"));
        Assert.Null(os.GetGuidance("Izracunati broj stranica"));
    }
}
