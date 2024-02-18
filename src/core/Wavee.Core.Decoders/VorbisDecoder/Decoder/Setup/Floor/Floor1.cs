using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Codebooks;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure;
using Wavee.Core.Decoders.VorbisDecoder.Infrastructure.BitReaders;

namespace Wavee.Core.Decoders.VorbisDecoder.Decoder.Setup.Floor;

internal sealed class Floor1 : IFloor
{
    private readonly Floor1Setup _setup;
    private bool _isUnused;
    private readonly uint[] _floorY;
    private readonly int[] _floorFinalY;
    private readonly bool[] _floorStep2Flag;

    private record Floor1Setup(
        int Floor1Partitions,
        byte[] Floor1PartitionClassList,
        Floor1Class[] Floor1Classes,
        byte Floor1Multiplier,
        List<uint> Floor1XList,
        List<(uint, uint)> Floor1XListNeighbors,
        List<byte> Floor1XListSortOrder
    );

    private Floor1(Floor1Setup setup, bool isUnused, uint[] floorY, int[] floorFinalY, bool[] floorStep2Flag)
    {
        _setup = setup;
        _isUnused = isUnused;
        _floorY = floorY;
        _floorFinalY = floorFinalY;
        _floorStep2Flag = floorStep2Flag;
    }

    public static Result<Floor1> Read(ref BitReaderRtlRef bs, byte maxCodebook)
    {
        var setupRes = ReadSetup(ref bs, maxCodebook);
        if (setupRes.IsFaulted)
            return new Result<Floor1>(setupRes.Error());
        var setup = setupRes.Success();
        var xlistLen = setup.Floor1XList.Count;
        var emptyFloorY = new uint[xlistLen];
        Array.Fill(emptyFloorY, 0u);

        var emptyFloorFinalY = new int[xlistLen];
        Array.Fill(emptyFloorFinalY, 0);

        var emptyFloorStep2Flag = new bool[xlistLen];
        Array.Fill(emptyFloorStep2Flag, false);
        return new Floor1(
            setup: setup,
            isUnused: false,
            floorY: emptyFloorY,
            floorFinalY: emptyFloorFinalY,
            floorStep2Flag: emptyFloorStep2Flag
        );
    }

    private static Result<Floor1Setup> ReadSetup(ref BitReaderRtlRef bs, byte maxCodebook)
    {
        // The number of partitions. 5-bit value, 0..31 range.
        //        let floor1_partitions = bs.read_bits_leq32(5)? as usize;
        var floor1partitionsRes = bs.ReadBitsLeq32(5);
        if (floor1partitionsRes.IsFaulted)
            return new Result<Floor1Setup>(floor1partitionsRes.Error());
        var floor1Partitions = floor1partitionsRes.Success();

        // Parition list of up-to 32 partitions (floor1_partitions), with each partition indicating
        // a 4-bit class (0..16) identifier.
        var floor1PartitionClassList = new byte[32];
        Array.Fill(floor1PartitionClassList, (byte)0);
        var floor1Classes = new Floor1Class[16];
        for (var i = 0; i < floor1Classes.Length; i++)
        {
            floor1Classes[i] = new Floor1Class();
        }

        if (floor1Partitions > 0)
        {
            byte maxClass = 0; // 4-bits, 0..15

            // Read the partition class list.
            for (int i = 0; i < floor1Partitions; i++)
            {
                var classIdx = floor1PartitionClassList[i];
                var classIdxRes = bs.ReadBitsLeq32(4).Map(x => (byte)x);
                if (classIdxRes.IsFaulted)
                    return new Result<Floor1Setup>(classIdxRes.Error());

                floor1PartitionClassList[i] = classIdxRes.Success();
                maxClass = Math.Max(maxClass, floor1PartitionClassList[i]);
            }

            var numClasses = maxClass + 1;

            for (int i = 0; i < numClasses; i++)
            {
                var classs = floor1Classes[i];
                var dimensionsRes = bs.ReadBitsLeq32(3).Map(x => (byte)(x + 1));
                var subClassRes = bs.ReadBitsLeq32(2).Map(x => (byte)x);
                if (dimensionsRes.IsFaulted || subClassRes.IsFaulted)
                    return new Result<Floor1Setup>(
                        dimensionsRes.IsFaulted ? dimensionsRes.Error() : subClassRes.Error());
                classs.Dimensions = dimensionsRes.Success();
                classs.SubclassBits = subClassRes.Success();

                if (classs.SubclassBits != 0)
                {
                    var masterbookRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
                    if (masterbookRes.IsFaulted)
                        return new Result<Floor1Setup>(masterbookRes.Error());
                    var masterbook = masterbookRes.Success();

                    if (masterbook >= maxCodebook)
                    {
                        return new Result<Floor1Setup>(new DecodeError("vorbis: floor1, invalid codebook for class"));
                    }

                    classs.Mainbook = masterbook;
                }

                var numSubclasses = 1 << classs.SubclassBits;

                for (int m = 0; m < numSubclasses; m++)
                {
                    var book = classs.Subbooks[m];
                    // Read the codebook number.
                    var bookRes = bs.ReadBitsLeq32(8).Map(x => (byte)x);
                    if (bookRes.IsFaulted)
                        return new Result<Floor1Setup>(bookRes.Error());
                    book = bookRes.Success();
                    classs.Subbooks[m] = book;

                    // A codebook number > 0 indicates a codebook is used.
                    if (book > 0)
                    {
                        // The actual codebook used is the number read minus one.
                        book -= 1;
                        classs.Subbooks[m] = book;
                        if (book >= maxCodebook)
                            return new Result<Floor1Setup>(
                                new DecodeError("vorbis: floor1, invalid codebook for subclass"));

                        classs.IsSubbookUsed |= (byte)(1 << m);
                    }
                }
            }
        }

        var floor1Multiplier = bs.ReadBitsLeq32(2).Map(x => (byte)(x + 1));
        var rangeBitsRes = bs.ReadBitsLeq32(4);
        if (floor1Multiplier.IsFaulted || rangeBitsRes.IsFaulted)
            return new Result<Floor1Setup>(
                floor1Multiplier.IsFaulted ? floor1Multiplier.Error() : rangeBitsRes.Error());
        var floor1MultiplierVal = floor1Multiplier.Success();
        var rangeBits = rangeBitsRes.Success();

        var floor1XList = new List<uint>();
        var floor1XListUnique = new System.Collections.Generic.HashSet<uint>();

        floor1XList.Add(0);
        floor1XList.Add((uint)(1 << rangeBits));

        for (int i = 0; i < floor1Partitions; i++)
        {
            var classIdx = floor1PartitionClassList[i];
            var classs = floor1Classes[classIdx];

            // No more than 65 elements are allowed.
            if ((floor1XList.Length() + classs.Dimensions) > 65)
            {
                return new Result<Floor1Setup>(new DecodeError("vorbis: floor1, x_list too long"));
            }

            for (int _ = 0; _ < classs.Dimensions; _++)
            {
                var xRes = bs.ReadBitsLeq32((uint)rangeBits);
                if (xRes.IsFaulted)
                    return new Result<Floor1Setup>(xRes.Error());
                var x = (uint)xRes.Success();

                // All elements in the x list must be unique.
                if (!floor1XListUnique.Add(x))
                    return new Result<Floor1Setup>(new DecodeError("vorbis: floor1, duplicate x value"));

                floor1XList.Add(x);
            }
        }

        var floor1XListNeighbors = new List<(uint, uint)>(floor1XList.Length());
        var floor1XListSortOrder = new List<byte>(floor1XList.Length());

        // Precompute neighbors.
        for (int i = 0; i < floor1XList.Length(); i++)
        {
            floor1XListNeighbors.Add(find_neighbors(floor1XList, i));
            floor1XListSortOrder.Add((byte)i);
        }

        // Precompute sort-order.
        floor1XListSortOrder.Sort((a, b) => floor1XList[a].CompareTo(floor1XList[b]));


        var floorType1 = new Floor1Setup(
            Floor1Partitions: floor1Partitions,
            Floor1PartitionClassList: floor1PartitionClassList,
            Floor1Classes: floor1Classes,
            Floor1Multiplier: floor1MultiplierVal,
            Floor1XList: floor1XList,
            Floor1XListNeighbors: floor1XListNeighbors,
            Floor1XListSortOrder: floor1XListSortOrder
        );

        return new Result<Floor1Setup>(floorType1);
    }

    private static (uint, uint) find_neighbors(List<uint> vec, int x)
    {
        var bound = vec[x];

        var low = uint.MinValue; //TODO: Should be -1?
        var high = uint.MaxValue;

        (uint, uint) res = (0, 0);

        // Sections 9.2.4 and 9.2.5
        for (int i = 0; i < x; i++)
        {
            uint xv = vec[i];
            // low_neighbor(v,x) finds the position n in vector [v] of the greatest value scalar element
            // for which n is less than x and vector [v] element n is less than vector [v] element [x].
            if (xv > low && xv < bound)
            {
                low = xv;
                res.Item1 = (uint)i;
            }

            // high_neighbor(v,x) finds the position n in vector [v] of the lowest value scalar element
            // for which n is less than x and vector [v] element n is greater than vector [v] element [x].
            if (xv < high && xv > bound)
            {
                high = xv;
                res.Item2 = (uint)i;
            }
        }

        return res;
    }

    public Result<Unit> ReadChannel(ref BitReaderRtlRef bs, VorbisCodebook[] codebooks)
    {
        // Assume the floor is unused until it is decoded successfully.
        _isUnused = true;

        // First bit marks if this floor is used. Exit early if it is not.
        bs.TryReadBool(out var isUsed);

        if (!isUsed) return new Result<Unit>(Unit.Default);

        // Section 7.3.2
        var range = GetRange(_setup.Floor1Multiplier);

        // The number of bits required to represent range.
        var rangeBits = ((uint)(range - 1)).ilog();

        bs.TryReadBits(rangeBits, out var floorY0Res);
        bs.TryReadBits(rangeBits, out var floorY1Res);
        if (floorY0Res.IsFaulted || floorY1Res.IsFaulted)
            return new Result<Unit>(
                floorY0Res.IsFaulted ? floorY0Res.Error() : floorY1Res.Error());
        var floorY0 = floorY0Res.Success();
        var floorY1 = floorY1Res.Success();
        _floorY[0] = (uint)floorY0;
        _floorY[1] = (uint)floorY1;

        var offset = 2;
        for (int i = 0; i < _setup.Floor1Partitions; i++)
        {
            var classIdx = _setup.Floor1PartitionClassList[i];
            var cl = _setup.Floor1Classes[classIdx];

            var cdim = cl.Dimensions;
            var cbits = cl.SubclassBits;
            var csub = (1 << cbits) - 1;

            uint cval = 0;

            if (cbits > 0)
            {
                var mainbookIdx = cl.Mainbook;
                var cvalRes = codebooks[mainbookIdx].ReadScalar(ref bs);
                if (cvalRes.IsFaulted)
                    return new Result<Unit>(cvalRes.Error());
                cval = (uint)cvalRes.Success();
            }

            for (int j = offset; j < (offset + cdim); j++)
            {
                var subclassIdx = cval & csub;

                // Is the sub-book used for this sub-class.
                var isSubbookUsed = (cl.IsSubbookUsed & (1 << (int)subclassIdx)) != 0;
                cval >>= cbits;

                if (isSubbookUsed)
                {
                    var subbookidx = cl.Subbooks[subclassIdx];
                    var readRes = codebooks[subbookidx].ReadScalar(ref bs);
                    if (readRes.IsFaulted)
                        return new Result<Unit>(readRes.Error());
                    var read = readRes.Success();
                    _floorY[j] = (uint)read;
                }
                else
                {
                    _floorY[j] = 0;
                }
            }

            offset += cdim;
        }

        // If this point is reached then the floor is used.
        _isUnused = false;

        return new Result<Unit>(Unit.Default);
    }

    public Result<Unit> Synthesis(byte bsExp, Span<float> chFloor)
    {
        SynthesisStep1();
        SynthesisStep2((uint)((1 << bsExp) >> 1), chFloor);

        return new Result<Unit>(Unit.Default);
    }

    public bool IsUnused() => _isUnused;

    private void SynthesisStep1()
    {
        // Step 1.
        var range = GetRange(_setup.Floor1Multiplier);

        _floorStep2Flag[0] = true;
        _floorStep2Flag[1] = true;

        _floorFinalY[0] = (int)_floorY[0];
        _floorFinalY[1] = (int)_floorY[1];

        for (int i = 2; i < _setup.Floor1XList.Count; i++)
        {
            // Find the neighbours.
            var (low, high) = _setup.Floor1XListNeighbors[i];

            var predicted = RenderPoint(
                _setup.Floor1XList[(int)low],
                _floorFinalY[(int)low],
                _setup.Floor1XList[(int)high],
                _floorFinalY[(int)high],
                _setup.Floor1XList[i]);

            var val = (int)_floorY[i];
            var highroom = ((int)range) - predicted;
            var lowroom = predicted;

            if (val != 0)
            {
                var room = 2 * lowroom;
                if (highroom < lowroom)
                    room = 2 * highroom;

                _floorStep2Flag[low] = true;
                _floorStep2Flag[high] = true;
                _floorStep2Flag[i] = true;

                if (val >= room)
                {
                    if (highroom > lowroom)
                    {
                        _floorFinalY[i] = val - lowroom + predicted;
                    }
                    else
                    {
                        _floorFinalY[i] = predicted - val + highroom - 1;
                    }
                }
                else
                {
                    if ((val & 1) == 1)
                    {
                        _floorFinalY[i] = predicted - ((val + 1) / 2);
                    }
                    else
                    {
                        _floorFinalY[i] = predicted + (val / 2);
                    }
                }
            }
            else
            {
                _floorStep2Flag[i] = false;
                _floorFinalY[i] = predicted;
            }
        }
    }


    private static uint GetRange(byte multiplier)
    {
        return (multiplier - 1) switch
        {
            0 => 256,
            1 => 128,
            2 => 86,
            3 => 64,
        };
    }

    private void SynthesisStep2(uint n, Span<float> floor)
    {
        var multiplier = (int)_setup.Floor1Multiplier;

        var floorFinalY0 = _floorFinalY[_setup.Floor1XListSortOrder[0]];

        uint hx = 0;
        int hy = 0;
        uint lx = 0;
        int ly = floorFinalY0 * multiplier;

        // Iterate in sort-order.
        for (int j = 1; j < _setup.Floor1XListSortOrder.Count; j++)
        {
            var x = _setup.Floor1XListSortOrder[j];
            if (_floorStep2Flag[x])
            {
                hy = _floorFinalY[x] * multiplier;
                hx = (uint)_setup.Floor1XList[x];

                if (_floorStep2Flag[x])
                {
                    RenderLineMulti(lx, ly, hx, hy, (int)n, floor);
                    lx = hx;
                    ly = hy;
                }

                lx = hx;
                ly = hy;
            }
        }

        if (hx < n)
        {
            RenderLineMulti(hx, hy, n, hy, (int)n, floor);
        }
    }

    static int RenderPoint(uint x0, int y0, uint x1, int y1, uint X)
    {
        var dy = y1 - y0;
        var adx = x1 - x0;
        var ady = Math.Abs(dy);
        var err = ady * (X - x0);
        var off = err / adx;
        if (dy < 0)
        {
            return (int)(y0 - off);
        }
        else
        {
            return (int)(y0 + off);
        }
    }

    static void RenderLineMulti(uint x0, int y0, uint x1, int y1, int n, Span<float> f)
    {
        var dy = y1 - y0;
        var adx = (int)(x1 - x0);

        var b = dy / adx;

        var y = y0;

        var sy = dy < 0 ? (b - 1) : (b + 1);
        var ady = Math.Abs(dy) - Math.Abs(b) * adx;
        f[(int)x0] = inverse_dB_table[y];

        int err = 0;

        var xbegin = (int)(x0 + 1);
        var xend = (int)Math.Min(n, x1);
        for (var i = xbegin; i < xend; i++)
        {
            err += ady;

            if (err >= adx)
            {
                err -= adx;
                y += sy;
            }
            else
            {
                y += b;
            }

            f[i] = inverse_dB_table[y];
        }
    }


    static readonly float[] inverse_dB_table =
    {
        1.0649863e-07f, 1.1341951e-07f, 1.2079015e-07f, 1.2863978e-07f,
        1.3699951e-07f, 1.4590251e-07f, 1.5538408e-07f, 1.6548181e-07f,
        1.7623575e-07f, 1.8768855e-07f, 1.9988561e-07f, 2.1287530e-07f,
        2.2670913e-07f, 2.4144197e-07f, 2.5713223e-07f, 2.7384213e-07f,
        2.9163793e-07f, 3.1059021e-07f, 3.3077411e-07f, 3.5226968e-07f,
        3.7516214e-07f, 3.9954229e-07f, 4.2550680e-07f, 4.5315863e-07f,
        4.8260743e-07f, 5.1396998e-07f, 5.4737065e-07f, 5.8294187e-07f,
        6.2082472e-07f, 6.6116941e-07f, 7.0413592e-07f, 7.4989464e-07f,
        7.9862701e-07f, 8.5052630e-07f, 9.0579828e-07f, 9.6466216e-07f,
        1.0273513e-06f, 1.0941144e-06f, 1.1652161e-06f, 1.2409384e-06f,
        1.3215816e-06f, 1.4074654e-06f, 1.4989305e-06f, 1.5963394e-06f,
        1.7000785e-06f, 1.8105592e-06f, 1.9282195e-06f, 2.0535261e-06f,
        2.1869758e-06f, 2.3290978e-06f, 2.4804557e-06f, 2.6416497e-06f,
        2.8133190e-06f, 2.9961443e-06f, 3.1908506e-06f, 3.3982101e-06f,
        3.6190449e-06f, 3.8542308e-06f, 4.1047004e-06f, 4.3714470e-06f,
        4.6555282e-06f, 4.9580707e-06f, 5.2802740e-06f, 5.6234160e-06f,
        5.9888572e-06f, 6.3780469e-06f, 6.7925283e-06f, 7.2339451e-06f,
        7.7040476e-06f, 8.2047000e-06f, 8.7378876e-06f, 9.3057248e-06f,
        9.9104632e-06f, 1.0554501e-05f, 1.1240392e-05f, 1.1970856e-05f,
        1.2748789e-05f, 1.3577278e-05f, 1.4459606e-05f, 1.5399272e-05f,
        1.6400004e-05f, 1.7465768e-05f, 1.8600792e-05f, 1.9809576e-05f,
        2.1096914e-05f, 2.2467911e-05f, 2.3928002e-05f, 2.5482978e-05f,
        2.7139006e-05f, 2.8902651e-05f, 3.0780908e-05f, 3.2781225e-05f,
        3.4911534e-05f, 3.7180282e-05f, 3.9596466e-05f, 4.2169667e-05f,
        4.4910090e-05f, 4.7828601e-05f, 5.0936773e-05f, 5.4246931e-05f,
        5.7772202e-05f, 6.1526565e-05f, 6.5524908e-05f, 6.9783085e-05f,
        7.4317983e-05f, 7.9147585e-05f, 8.4291040e-05f, 8.9768747e-05f,
        9.5602426e-05f, 0.00010181521f, 0.00010843174f, 0.00011547824f,
        0.00012298267f, 0.00013097477f, 0.00013948625f, 0.00014855085f,
        0.00015820453f, 0.00016848555f, 0.00017943469f, 0.00019109536f,
        0.00020351382f, 0.00021673929f, 0.00023082423f, 0.00024582449f,
        0.00026179955f, 0.00027881276f, 0.00029693158f, 0.00031622787f,
        0.00033677814f, 0.00035866388f, 0.00038197188f, 0.00040679456f,
        0.00043323036f, 0.00046138411f, 0.00049136745f, 0.00052329927f,
        0.00055730621f, 0.00059352311f, 0.00063209358f, 0.00067317058f,
        0.00071691700f, 0.00076350630f, 0.00081312324f, 0.00086596457f,
        0.00092223983f, 0.00098217216f, 0.0010459992f, 0.0011139742f,
        0.0011863665f, 0.0012634633f, 0.0013455702f, 0.0014330129f,
        0.0015261382f, 0.0016253153f, 0.0017309374f, 0.0018434235f,
        0.0019632195f, 0.0020908006f, 0.0022266726f, 0.0023713743f,
        0.0025254795f, 0.0026895994f, 0.0028643847f, 0.0030505286f,
        0.0032487691f, 0.0034598925f, 0.0036847358f, 0.0039241906f,
        0.0041792066f, 0.0044507950f, 0.0047400328f, 0.0050480668f,
        0.0053761186f, 0.0057254891f, 0.0060975636f, 0.0064938176f,
        0.0069158225f, 0.0073652516f, 0.0078438871f, 0.0083536271f,
        0.0088964928f, 0.009474637f, 0.010090352f, 0.010746080f,
        0.011444421f, 0.012188144f, 0.012980198f, 0.013823725f,
        0.014722068f, 0.015678791f, 0.016697687f, 0.017782797f,
        0.018938423f, 0.020169149f, 0.021479854f, 0.022875735f,
        0.024362330f, 0.025945531f, 0.027631618f, 0.029427276f,
        0.031339626f, 0.033376252f, 0.035545228f, 0.037855157f,
        0.040315199f, 0.042935108f, 0.045725273f, 0.048696758f,
        0.051861348f, 0.055231591f, 0.058820850f, 0.062643361f,
        0.066714279f, 0.071049749f, 0.075666962f, 0.080584227f,
        0.085821044f, 0.091398179f, 0.097337747f, 0.10366330f,
        0.11039993f, 0.11757434f, 0.12521498f, 0.13335215f,
        0.14201813f, 0.15124727f, 0.16107617f, 0.17154380f,
        0.18269168f, 0.19456402f, 0.20720788f, 0.22067342f,
        0.23501402f, 0.25028656f, 0.26655159f, 0.28387361f,
        0.30232132f, 0.32196786f, 0.34289114f, 0.36517414f,
        0.38890521f, 0.41417847f, 0.44109412f, 0.46975890f,
        0.50028648f, 0.53279791f, 0.56742212f, 0.60429640f,
        0.64356699f, 0.68538959f, 0.72993007f, 0.77736504f,
        0.82788260f, 0.88168307f, 0.9389798f, 1.0f
    };
}

internal class Floor1Class
{
    public Floor1Class()
    {
        Subbooks = new byte[8];
        Array.Fill(Subbooks, (byte)0);
    }

    public byte Dimensions { get; set; }
    public byte SubclassBits { get; set; }
    public byte Mainbook { get; set; }
    public byte[] Subbooks { get; set; }
    public byte IsSubbookUsed { get; set; }
}