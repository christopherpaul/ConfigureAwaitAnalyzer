using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<string> M()
        {
            return await N();
        }

        async ValueTask<string> N()
        {
            return string.Empty;
        }
    }
}
