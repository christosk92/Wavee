namespace Wavee.Spfy.Utils;

internal class Shannon
{
    /*
     * Fold is how many register cycles need to be performed after combining the
     * last byte of key and non-linear feedback, before every byte depends on every
     * byte of the key. This depends on the feedback and nonlinear functions, and
     * on where they are combined into the register. Making it same as the register
     * length is a safe and conservative choice.
     */
    private static readonly int N = 16;
    private static readonly int FOLD = N; /* How many iterations of folding to do. */
    private static readonly int INITKONST = 0x6996c53a; /* Value of konst to use during key loading. */
    private static readonly int KEYP = 13; /* Where to insert key/MAC/counter words. */
    private readonly int[] CRC; /* Working storage for CRC accumulation. */
    private readonly int[] initR; /* Saved register contents. */

    private readonly int[] R; /* Working storage for the shift register. */
    private int konst; /* Key dependant semi-constant. */
    private int mbuf; /* Partial word MAC buffer. */
    private int nbuf; /* Number of part-word stream bits buffered. */
    private int sbuf; /* Encryption buffer. */

    /**
	     * Create a new instance of the Shannon stream-cipher.
	     */
    public Shannon(ReadOnlySpan<byte> key)
    {
        /* Registers with length N. */
        R = new int[N];
        CRC = new int[N];
        initR = new int[N];
        this.key(key);
    }


    private int IntRotateLeft(int x, int n)
    {
        var ux = (uint)(x & 0xFFFFFFFF);
        return (int)(ux << n | ux >> 32 - n);
    }

    /* Nonlinear transform (sbox) of a word. There are two slightly different combinations. */
    private int sbox(int i)
    {
        i ^= IntRotateLeft(i, 5) | IntRotateLeft(i, 7);
        i ^= IntRotateLeft(i, 19) | IntRotateLeft(i, 22);

        return i;
    }

    private int sbox2(int i)
    {
        i ^= IntRotateLeft(i, 7) | IntRotateLeft(i, 22);
        i ^= IntRotateLeft(i, 5) | IntRotateLeft(i, 19);

        return i;
    }

    /* Cycle the contents of the register and calculate output word in sbuf. */
    private void cycle()
    {
        /* Temporary variable. */
        int t;

        /* Nonlinear feedback function. */
        t = R[12] ^ R[13] ^ konst;
        t = sbox(t) ^ IntRotateLeft(R[0], 1);

        /* Shift register. */
        for (var i = 1; i < N; i++) R[i - 1] = R[i];

        R[N - 1] = t;

        t = sbox2(R[2] ^ R[15]);
        R[0] ^= t;
        sbuf = t ^ R[8] ^ R[12];
    }

    /*
     * The Shannon MAC function is modelled after the concepts of Phelix and SHA.
     * Basically, words to be accumulated in the MAC are incorporated in two
     * different ways:
     * 1. They are incorporated into the stream cipher register at a place
     *    where they will immediately have a nonlinear effect on the state.
     * 2. They are incorporated into bit-parallel CRC-16 registers; the
     *    contents of these registers will be used in MAC finalization.
     */

    /* 
     * Accumulate a CRC of input words, later to be fed into MAC.
     * This is actually 32 parallel CRC-16s, using the IBM CRC-16
     * polynomian x^16 + x^15 + x^2 + 1
     */
    private void crcFunc(int i)
    {
        /* Temporary variable. */
        int t;

        /* Accumulate CRC of input. */
        t = CRC[0] ^ CRC[2] ^ CRC[15] ^ i;

        for (var j = 1; j < N; j++) CRC[j - 1] = CRC[j];

        CRC[N - 1] = t;
    }

    /* Normal MAC word processing: do both stream register and CRC. */
    private void macFunc(int i)
    {
        crcFunc(i);

        R[KEYP] ^= i;
    }

    /* Initialize to known state. */
    private void initState()
    {
        /* Register initialized to Fibonacci numbers. */
        ////Fast loading
        //if (N == 16)
        //{
        //    this.R = new int[16] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987 };
        //}
        //else
        //{

        R[0] = 1;
        R[1] = 1;

        for (var i = 2; i < N; i++) R[i] = R[i - 1] + R[i - 2];
        //}

        /* Initialization constant. */
        konst = INITKONST;
    }

    /* Save the current register state. */
    private void saveState()
    {
        for (var i = 0; i < N; i++) initR[i] = R[i];
    }

    /* Inisialize to previously saved register state. */
    private void reloadState()
    {
        for (var i = 0; i < N; i++) R[i] = initR[i];
    }

    /* Initialize 'konst'. */
    private void genKonst()
    {
        konst = R[0];
    }

    /* Load key material into the register. */
    private void addKey(int k)
    {
        R[KEYP] ^= k;
    }

    /* Extra nonlinear diffusion of register for key and MAC. */
    private void diffuse()
    {
        for (var i = 0; i < FOLD; i++) cycle();
    }

    /*
     * Common actions for loading key material.
     * Allow non-word-multiple key and nonce material.
     * Note: Also initializes the CRC register as a side effect.
     */
    private void loadKey(ReadOnlySpan<byte> key)
    {
        var extra = new byte[4];
        int i, j;
        int t;

        /* Start folding key. */
        for (i = 0; i < (key.Length & ~0x03); i += 4)
        {
            /* Shift 4 bytes into one word. */
            t = (key[i + 3] & 0xFF) << 24 |
                (key[i + 2] & 0xFF) << 16 |
                (key[i + 1] & 0xFF) << 8 |
                key[i] & 0xFF;

            /* Insert key word at index 13. */
            addKey(t);

            /* Cycle register. */
            cycle();
        }

        /* If there were any extra bytes, zero pad to a word. */
        if (i < key.Length)
        {
            /* i remains unchanged at start of loop. */
            for (j = 0; i < key.Length; i++) extra[j++] = key[i];

            /* j remains unchanged at start of loop. */
            for (; j < 4; j++) extra[j] = 0;

            /* Shift 4 extra bytes into one word. */
            t = (extra[3] & 0xFF) << 24 |
                (extra[2] & 0xFF) << 16 |
                (extra[1] & 0xFF) << 8 |
                extra[0] & 0xFF;

            /* Insert key word at index 13. */
            addKey(t);

            /* Cycle register. */
            cycle();
        }

        /* Also fold in the length of the key. */
        addKey(key.Length);

        /* Cycle register. */
        cycle();

        /* Save a copy of the register. */
        for (i = 0; i < N; i++) CRC[i] = R[i];

        /* Now diffuse. */
        diffuse();

        /* Now XOR the copy back -- makes key loading irreversible. */
        for (i = 0; i < N; i++) R[i] ^= CRC[i];
    }

    /* Set key */
    public void key(ReadOnlySpan<byte> key)
    {
        /* Initializet known state. */
        initState();

        /* Load key material. */
        loadKey(key);

        /* In case we proceed to stream generation. */
        genKonst();

        /* Save register state. */
        saveState();

        /* Set 'nbuf' value to zero. */
        nbuf = 0;
    }

    /* Set IV */
    public void Nonce(ReadOnlySpan<byte> nonce)
    {
        /* Reload register state. */
        reloadState();

        /* Set initialization constant. */
        konst = INITKONST;

        /* Load "IV" material. */
        loadKey(nonce);

        /* Set 'konst'. */
        genKonst();

        /* Set 'nbuf' value to zero. */
        nbuf = 0;
    }

    /*
     * XOR pseudo-random bytes into buffer.
     * Note: doesn't play well with MAC functions. 
     */
    public void stream(byte[] buffer)
    {
        int i = 0, j, n = buffer.Length;

        /* Handle any previously buffered bytes. */
        while (nbuf != 0 && n != 0)
        {
            buffer[i++] ^= (byte)(sbuf & 0xFF);

            sbuf >>= 8;
            nbuf -= 8;

            n--;
        }

        /* Handle whole words. */
        j = n & ~0x03;

        while (i < j)
        {
            /* Cycle register. */
            cycle();

            /* XOR word. */
            buffer[i + 3] ^= (byte)(sbuf >> 24 & 0xFF);
            buffer[i + 2] ^= (byte)(sbuf >> 16 & 0xFF);
            buffer[i + 1] ^= (byte)(sbuf >> 8 & 0xFF);
            buffer[i] ^= (byte)(sbuf & 0xFF);

            i += 4;
        }

        /* Handle any trailing bytes. */
        n &= 0x03;

        if (n != 0)
        {
            /* Cycle register. */
            cycle();

            nbuf = 32;

            while (nbuf != 0 && n != 0)
            {
                buffer[i++] ^= (byte)(sbuf & 0xFF);

                sbuf >>= 8;
                nbuf -= 8;

                n--;
            }
        }
    }

    /*
     * Accumulate words into MAC without encryption.
     * Note that plaintext is accumulated for MAC.
     */
    public void macOnly(byte[] buffer)
    {
        int i = 0, j, n = buffer.Length;
        int t;

        /* Handle any previously buffered bytes. */
        if (nbuf != 0)
        {
            while (nbuf != 0 && n != 0)
            {
                mbuf ^= buffer[i++] << 32 - nbuf;
                nbuf -= 8;

                n--;
            }

            /* Not a whole word yet. */
            if (nbuf != 0) return;

            /* LFSR already cycled. */
            macFunc(mbuf);
        }

        /* Handle whole words. */
        j = n & ~0x03;

        while (i < j)
        {
            /* Cycle register. */
            cycle();

            /* Shift 4 bytes into one word. */
            t = (buffer[i + 3] & 0xFF) << 24 |
                (buffer[i + 2] & 0xFF) << 16 |
                (buffer[i + 1] & 0xFF) << 8 |
                buffer[i] & 0xFF;

            macFunc(t);

            i += 4;
        }

        /* Handle any trailing bytes. */
        n &= 0x03;

        if (n != 0)
        {
            /* Cycle register. */
            cycle();

            mbuf = 0;
            nbuf = 32;

            while (nbuf != 0 && n != 0)
            {
                mbuf ^= buffer[i++] << 32 - nbuf;
                nbuf -= 8;

                n--;
            }
        }
    }

    /*
     * Combined MAC and encryption.
     * Note that plaintext is accumulated for MAC.
     */
    public void Encrypt(Span<byte> buffer)
    {
        encrypt(buffer, buffer.Length);
    }

    /*
     * Combined MAC and encryption.
     * Note that plaintext is accumulated for MAC.
     */
    private void encrypt(Span<byte> buffer, int n)
    {
        int i = 0, j;
        int t;

        /* Handle any previously buffered bytes. */
        if (nbuf != 0)
        {
            while (nbuf != 0 && n != 0)
            {
                mbuf ^= (buffer[i] & 0xFF) << 32 - nbuf;
                buffer[i] ^= (byte)(sbuf >> 32 - nbuf & 0xFF);

                i++;

                nbuf -= 8;

                n--;
            }

            /* Not a whole word yet. */
            if (nbuf != 0) return;

            /* LFSR already cycled. */
            macFunc(mbuf);
        }

        /* Handle whole words. */
        j = n & ~0x03;

        while (i < j)
        {
            /* Cycle register. */
            cycle();

            /* Shift 4 bytes into one word. */
            t = (buffer[i + 3] & 0xFF) << 24 |
                (buffer[i + 2] & 0xFF) << 16 |
                (buffer[i + 1] & 0xFF) << 8 |
                buffer[i] & 0xFF;

            macFunc(t);

            t ^= sbuf;

            /* Put word into byte buffer. */
            buffer[i + 3] = (byte)(t >> 24 & 0xFF);
            buffer[i + 2] = (byte)(t >> 16 & 0xFF);
            buffer[i + 1] = (byte)(t >> 8 & 0xFF);
            buffer[i] = (byte)(t & 0xFF);

            i += 4;
        }

        /* Handle any trailing bytes. */
        n &= 0x03;

        if (n != 0)
        {
            /* Cycle register. */
            cycle();

            mbuf = 0;
            nbuf = 32;

            while (nbuf != 0 && n != 0)
            {
                mbuf ^= (buffer[i] & 0xFF) << 32 - nbuf;
                buffer[i] ^= (byte)(sbuf >> 32 - nbuf & 0xFF);

                i++;

                nbuf -= 8;

                n--;
            }
        }
    }

    /*
     * Combined MAC and decryption.
     * Note that plaintext is accumulated for MAC.
     */
    public void Decrypt(Span<byte> buffer)
    {
        decrypt(buffer, buffer.Length);
    }

    /*
     * Combined MAC and decryption.
     * Note that plaintext is accumulated for MAC.
     */
    private void decrypt(Span<byte> buffer, int n)
    {
        int i = 0, j;
        int t;

        /* Handle any previously buffered bytes. */
        if (nbuf != 0)
        {
            while (nbuf != 0 && n != 0)
            {
                buffer[i] ^= (byte)(sbuf >> 32 - nbuf & 0xFF);
                mbuf ^= (buffer[i] & 0xFF) << 32 - nbuf;

                i++;

                nbuf -= 8;

                n--;
            }

            /* Not a whole word yet. */
            if (nbuf != 0) return;

            /* LFSR already cycled. */
            macFunc(mbuf);
        }

        /* Handle whole words. */
        j = n & ~0x03;

        while (i < j)
        {
            /* Cycle register. */
            cycle();

            /* Shift 4 bytes into one word. */
            t = (buffer[i + 3] & 0xFF) << 24 |
                (buffer[i + 2] & 0xFF) << 16 |
                (buffer[i + 1] & 0xFF) << 8 |
                buffer[i] & 0xFF;

            t ^= sbuf;

            macFunc(t);

            /* Put word into byte buffer. */
            buffer[i + 3] = (byte)(t >> 24 & 0xFF);
            buffer[i + 2] = (byte)(t >> 16 & 0xFF);
            buffer[i + 1] = (byte)(t >> 8 & 0xFF);
            buffer[i] = (byte)(t & 0xFF);

            i += 4;
        }

        /* Handle any trailing bytes. */
        n &= 0x03;

        if (n != 0)
        {
            /* Cycle register. */
            cycle();

            mbuf = 0;
            nbuf = 32;

            while (nbuf != 0 && n != 0)
            {
                buffer[i] ^= (byte)(sbuf >> 32 - nbuf & 0xFF);
                mbuf ^= (buffer[i] & 0xFF) << 32 - nbuf;

                i++;

                nbuf -= 8;

                n--;
            }
        }
    }

    /*
     * Having accumulated a MAC, finish processing and return it.
     * Note that any unprocessed bytes are treated as if they were
     * encrypted zero bytes, so plaintext (zero) is accumulated.
     */
    public void Finish(Span<byte> buffer)
    {
        finish(buffer, buffer.Length);
    }

    /*
     * Having accumulated a MAC, finish processing and return it.
     * Note that any unprocessed bytes are treated as if they were
     * encrypted zero bytes, so plaintext (zero) is accumulated.
     */
    private void finish(Span<byte> buffer, int n)
    {
        int i = 0, j;

        /* Handle any previously buffered bytes. */
        if (nbuf != 0)
            /* LFSR already cycled. */
            macFunc(mbuf);

        /*
         * Perturb the MAC to mark end of input.
         * Note that only the stream register is updated, not the CRC.
         * This is an action that can't be duplicated by passing in plaintext,
         * hence defeating any kind of extension attack.
         */
        cycle();
        addKey(INITKONST ^ nbuf << 3);

        nbuf = 0;

        /* Now add the CRC to the stream register and diffuse it. */
        for (j = 0; j < N; j++) R[j] ^= CRC[j];

        diffuse();

        /* Produce output from the stream buffer. */
        while (n > 0)
        {
            cycle();

            if (n >= 4)
            {
                /* Put word into byte buffer. */
                buffer[i + 3] = (byte)(sbuf >> 24 & 0xFF);
                buffer[i + 2] = (byte)(sbuf >> 16 & 0xFF);
                buffer[i + 1] = (byte)(sbuf >> 8 & 0xFF);
                buffer[i] = (byte)(sbuf & 0xFF);

                n -= 4;
                i += 4;
            }
            else
            {
                for (j = 0; j < n; j++) buffer[i + j] = (byte)(sbuf >> i * 8 & 0xFF);

                break;
            }
        }
    }

    public void Nonce(uint val)
    {
        Nonce(ToBytes(val));
    }

    private static ReadOnlySpan<byte> ToBytes(uint i)
    {
        Span<byte> b = new byte[4];
        b[0] = (byte)(i >> 24);
        b[1] = (byte)(i >> 16);
        b[2] = (byte)(i >> 8);
        b[3] = (byte)i;
        return b;
    }
}