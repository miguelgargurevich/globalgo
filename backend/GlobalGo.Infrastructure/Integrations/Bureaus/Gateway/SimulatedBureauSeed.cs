namespace GlobalGo.Infrastructure.Integrations.Bureaus.Gateway;

internal static class SimulatedBureauSeed
{
    public static int FromDni(string dni)
    {
        unchecked
        {
            var hash = 17;

            foreach (var ch in dni)
            {
                hash = (hash * 31) + ch;
            }

            return Math.Abs(hash);
        }
    }
}
