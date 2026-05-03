namespace Features.Unit.Domain
{
    public enum AssemblyForm
    {
        Unspecified,
        Tower,
        Shoulder,
        Humanoid
    }

    public static class UnitPartCompatibility
    {
        public static bool AreAssemblyFormsCompatible(AssemblyForm frameForm, AssemblyForm firepowerForm)
        {
            return frameForm == AssemblyForm.Unspecified ||
                   firepowerForm == AssemblyForm.Unspecified ||
                   frameForm == firepowerForm;
        }
    }
}
