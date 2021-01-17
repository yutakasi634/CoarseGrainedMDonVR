internal class HarmonicPotential : PotentailBase
{
    internal float m_V0;
    internal float m_ScaledKs;
    internal List<Rigidbody> m_RigidPair;

    internal HarmonicPotential(float v0, float k, List<Rigidbody> rigid_pair, float timescale)
    {
        m_V0        = v0;
        m_ScaledKs  = k * timescale * timescale;
        m_RigidPair = rigid_pair;
    }

    internal override float potential(float r)
    {
        float r_v0 = r - v0;
        return m_ScaledK * r_v0 * r_v0;
    }

    internal override float derivative(float r)
    {
        float r_v0 = r - v0;
        return 2.0f * m_ScaledK * r_v0;
    }
}
