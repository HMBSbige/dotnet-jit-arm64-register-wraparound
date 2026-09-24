using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;

public class RegisterWraparoundTests
{
    [Test]
    public async Task IdentityLookupPreservesValue()
    {
        await Assert.That(AdvSimd.Arm64.IsSupported).IsTrue();
        uint actual;
        unsafe
        {
            byte* table = stackalloc byte[256];
            for (int i = 0; i < 256; i++)
            {
                table[i] = (byte)i;
            }

            actual = Run(table, 0xF7F8FBFE);
        }

        await Assert.That(actual).IsEqualTo(0xF7F8FC06u);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe uint Run(byte* p, uint value)
    {
        for (int i = 0; i < 8; i++)
        {
            var (a0, a1, a2, a3) = AdvSimd.Arm64.Load4xVector128(p);
            var (b0, b1, b2, b3) = AdvSimd.Arm64.Load4xVector128(p + 64);
            var (c0, c1, c2, c3) = AdvSimd.Arm64.Load4xVector128(p + 128);
            var (d0, d1, d2, d3) = AdvSimd.Arm64.Load4xVector128(p + 192);
            var stride = Vector128.Create((byte)64);
            var index = Vector128.CreateScalar(value).AsByte();
            var result = AdvSimd.Arm64.VectorTableLookup((a0, a1, a2, a3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (b0, b1, b2, b3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (c0, c1, c2, c3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (d0, d1, d2, d3), index);
            value = result.AsUInt32().ToScalar() + 1;
        }

        return value;
    }
}
