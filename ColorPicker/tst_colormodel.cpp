#include <gtest/gtest.h>
#include "colormodel.h"

namespace {
constexpr double kEps = 1e-9;     // точные тождества (round-trip, соотношения)
constexpr double kEpsTab = 1e-4;  // сверка с табличными константами sRGB
void expectNear(const Vec3& a, const Vec3& b, double eps, const char* ctx)
{
    EXPECT_NEAR(a.x, b.x, eps) << ctx << " (X)";
    EXPECT_NEAR(a.y, b.y, eps) << ctx << " (Y)";
    EXPECT_NEAR(a.z, b.z, eps) << ctx << " (Z)";
}
} // namespace

// RGB(255,0,0) -> XYZ: яркость и хроматичность первичного цвета
TEST(ColorMath, Red255ToXyz)
{
    ColorModel m;
    m.setColorFromRgb8(255, 0, 0);
    const Vec3 xyz = m.xyz();
    EXPECT_NEAR(xyz.y, 0.2126729, kEpsTab);
    EXPECT_NEAR(xyz.x / xyz.y, 0.64 / 0.33, kEps);
    EXPECT_NEAR(xyz.z / xyz.y, 0.03 / 0.33, kEps);
}

// Сумма первичных = белая точка D65; серый идёт через sRGB-трансфер
TEST(ColorMath, PrimariesSumAndGray)
{
    ColorModel m;
    Vec3 sum(0, 0, 0);
    const Vec3 prim[3] = {{1,0,0}, {0,1,0}, {0,0,1}};
    for (const Vec3& p : prim) {
        m.setColorFromRgb(p);
        sum.x += m.xyz().x; sum.y += m.xyz().y; sum.z += m.xyz().z;
    }
    expectNear(sum, Vec3(0.31272/0.32903, 1.0, 0.35825/0.32903), kEps, "R+G+B");
    m.setColorFromRgb(Vec3(0.5, 0.5, 0.5));
    EXPECT_NEAR(m.xyz().y, 0.2140411, 1e-6); // srgbDecode(0.5)
}

// Round-trip RGB -> XYZ -> RGB внутри охвата
TEST(ColorMath, XyzRoundTrip)
{
    ColorModel m;
    m.setColorFromRgb(Vec3(0.3, 0.4, 0.7));
    const ColorModel::GamutFit fit = m.xyzToRgb(m.xyz());
    EXPECT_FALSE(fit.outOfGamut || fit.modified);
    expectNear(fit.encoded, Vec3(0.3, 0.4, 0.7), kEps, "round-trip");
}

// HSL: первичные цвета, серый и «развёрнутый» оттенок
TEST(ColorMath, HslPrimariesAndHue)
{
    expectNear(ColorModel::hslToRgb(Vec3(0.0, 1.0, 0.5)), Vec3(1,0,0), kEps, "H=0");
    expectNear(ColorModel::hslToRgb(Vec3(1./3., 1.0, 0.5)), Vec3(0,1,0), kEps, "H=120");
    expectNear(ColorModel::hslToRgb(Vec3(0.0, 0.0, 0.6)), Vec3(.6,.6,.6), kEps, "gray");
    ColorModel m;
    m.setColorFromHsl(Vec3(1.25, 0.6, 0.5));
    EXPECT_NEAR(m.hsl().x, 1.25, kEps);
    expectNear(m.rgb(), ColorModel::hslToRgb(Vec3(0.25, 0.6, 0.5)), kEps, "wrap");
}

// Охват: Clipping обрезает каналы, Scaling сохраняет яркость Y
TEST(Gamut, ClippingAndScaling)
{
    ColorModel m;
    const Vec3 bad(0.0, 0.0, 1.0);
    ColorModel::GamutFit f = m.xyzToRgb(bad);
    EXPECT_TRUE(f.outOfGamut && f.modified);
    EXPECT_NEAR(f.linear.x, 0.0, 1e-12);
    EXPECT_NEAR(f.linear.z, 1.0, 1e-12);
    m.setGamutStrategy(ColorModel::GamutStrategy::Scaling);
    f = m.xyzToRgb(bad);
    EXPECT_NEAR(f.linear.y, 0.041556, kEpsTab);
    EXPECT_NEAR(f.linear.x, 0.0, 1e-9);
    EXPECT_LE(compMax(f.linear), 1.0 + 1e-12);
}

// Иллюминант: белый RGB равен белой точке источника
TEST(Illuminant, WhitePoint)
{
    ColorModel m;
    m.setIlluminant(ColorModel::Illuminant::D50);
    m.setColorFromRgb8(255, 255, 255);
    expectNear(m.xyz(), Vec3(0.34567/0.3585, 1.0, 0.29583/0.3585), kEps, "D50");
    EXPECT_NEAR(m.xyzChannelMax(), 1.0, kEps);
    EXPECT_TRUE(m.matrixDebugString().contains(QStringLiteral("D50")));
}

// Qt: colorChanged стреляет только при реальном изменении
TEST(QtIntegration, ColorChangedSignal)
{
    ColorModel m;
    int n = 0;
    QObject::connect(&m, &ColorModel::colorChanged, [&n]{ ++n; });
    m.setIlluminant(ColorModel::Illuminant::D65); // то же значение — нет сигнала
    EXPECT_EQ(n, 0);
    m.setColorFromRgb8(255, 0, 0);
    EXPECT_EQ(n, 1);
}