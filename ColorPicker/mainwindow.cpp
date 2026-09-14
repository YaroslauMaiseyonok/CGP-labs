#include "mainwindow.h"
#include "channelrow.h"
#include "gradientslider.h"
#include "colorfield.h"

#include <QGroupBox>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QLabel>
#include <QLineEdit>
#include <QComboBox>
#include <QPushButton>
#include <QMessageBox>
#include <QRegularExpressionValidator>
#include <QColorDialog>
#include <QSizePolicy>

MainWindow::MainWindow(QWidget* parent)
    : QMainWindow(parent)
{
    rows.resize(GroupCount);
    buildUi();
    connectAll();
    syncFromModel();
}

ChannelRow* MainWindow::addRow(QVBoxLayout* box, int group, const QString& name,
                               double min, double max, int decimals, const QString& suffix)
{
    ChannelRow* row = new ChannelRow(name, min, max, decimals, suffix, this);
    box->addWidget(row);
    rows[group].append(row);
    return row;
}

void MainWindow::buildUi()
{
    auto* central = new QWidget(this);

    auto* mainLayout = new QVBoxLayout(central);
    mainLayout->setContentsMargins(10, 10, 10, 10);
    mainLayout->setSpacing(10);

    auto* root = new QHBoxLayout();
    root->setSpacing(10);

    mainLayout->addLayout(root, 1);

    setCentralWidget(central);

    auto* left = new QVBoxLayout;
    left->setSpacing(8);

    auto* cfg = new QHBoxLayout;
    cfg->addWidget(new QLabel(QStringLiteral("Источник:"), this));
    illuminantCombo = new QComboBox(this);
    illuminantCombo->addItem(QStringLiteral("D65"), int(ColorModel::Illuminant::D65));
    illuminantCombo->addItem(QStringLiteral("D50"), int(ColorModel::Illuminant::D50));
    illuminantCombo->addItem(QStringLiteral("E"), int(ColorModel::Illuminant::E));
    cfg->addWidget(illuminantCombo);
    cfg->addSpacing(10);
    cfg->addWidget(new QLabel(QStringLiteral("Охват:"), this));
    strategyCombo = new QComboBox(this);
    strategyCombo->addItem(QStringLiteral("Clipping"), int(ColorModel::GamutStrategy::Clipping));
    strategyCombo->addItem(QStringLiteral("Scaling"), int(ColorModel::GamutStrategy::Scaling));
    cfg->addWidget(strategyCombo);
    cfg->addStretch(1);
    left->addLayout(cfg);

    auto makeGroup = [this](const QString& title) {
        auto* gb = new QGroupBox(title, this);
        gb->setLayout(new QVBoxLayout);
        return gb;
    };
    auto vboxOf = [](QGroupBox* gb) { return qobject_cast<QVBoxLayout*>(gb->layout()); };

    QGroupBox* rgbBox = makeGroup(QStringLiteral("RGB"));
    addRow(vboxOf(rgbBox), RGB, "R", 0, 255, 0, QString());
    addRow(vboxOf(rgbBox), RGB, "G", 0, 255, 0, QString());
    addRow(vboxOf(rgbBox), RGB, "B", 0, 255, 0, QString());
    left->addWidget(rgbBox);

    QGroupBox* xyzBox = makeGroup(QStringLiteral("XYZ"));
    addRow(vboxOf(xyzBox), XYZ, "X", 0, 1.1, 4, QString());
    addRow(vboxOf(xyzBox), XYZ, "Y", 0, 1.1, 4, QString());
    addRow(vboxOf(xyzBox), XYZ, "Z", 0, 1.1, 4, QString());
    left->addWidget(xyzBox);

    QGroupBox* hslBox = makeGroup(QStringLiteral("HSL"));
    addRow(vboxOf(hslBox), HSL, "H", 0, 360, 1, QStringLiteral("°"));
    addRow(vboxOf(hslBox), HSL, "S", 0, 100, 1, QStringLiteral("%"));
    addRow(vboxOf(hslBox), HSL, "L", 0, 100, 1, QStringLiteral("%"));
    left->addWidget(hslBox);

    left->addStretch(1);
    root->addLayout(left, 1);

    auto* rightPanel = new QWidget(this);
    rightPanel->setFixedWidth(280);
    auto* right = new QVBoxLayout(rightPanel);
    right->setContentsMargins(0, 0, 0, 0);
    right->setSpacing(8);

    preview = new QLabel(rightPanel);
    preview->setFixedHeight(70);
    right->addWidget(preview);

    hexEdit = new QLineEdit(rightPanel);
    hexEdit->setMaxLength(7);
    hexEdit->setValidator(new QRegularExpressionValidator(
        QRegularExpression(QStringLiteral("^#?[0-9a-fA-F]{6}$")), this));
    right->addWidget(hexEdit);

    warnLabel = new QLabel(rightPanel);
    warnLabel->setFixedHeight(32);
    warnLabel->setWordWrap(true);
    warnLabel->setStyleSheet(QStringLiteral("QLabel { color: #b26a00; }"));
    right->addWidget(warnLabel);

    field = new ColorField(rightPanel);
    right->addWidget(field, 1);

    hueSlider = new GradientSlider(rightPanel);
    QGradientStops rainbow;
    for (int i = 0; i <= 64; ++i) {
        const double t = i / 64.0;
        rainbow.append(qMakePair(qreal(t), toQColor(ColorModel::hslToRgb(Vec3(t, 1.0, 0.5)))));
    }
    hueSlider->setStops(rainbow);
    right->addWidget(hueSlider);

    /*auto* matrixBtn = new QPushButton(QStringLiteral("Матрицы перехода…"), rightPanel);
    right->addWidget(matrixBtn);
    connect(matrixBtn, &QPushButton::clicked, this, [this] {
        QMessageBox::information(this, QStringLiteral("Матрицы перехода"),
                                 model.matrixDebugString());
    });*/

    root->addWidget(rightPanel);

    chooseColorButton = new QPushButton(QStringLiteral("Выбрать цвет…"), central);
    chooseColorButton->setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Fixed);
    mainLayout->addWidget(chooseColorButton);
}

void MainWindow::connectAll()
{
    for (int g = 0; g < GroupCount; ++g)
        for (int i = 0; i < rows[g].size(); ++i)
            connect(rows[g][i], &ChannelRow::valueChanged,
                    this, [this, g, i](double) { onChannelEdited(g, i); });

    connect(&model, &ColorModel::colorChanged, this, &MainWindow::syncFromModel);

    connect(illuminantCombo, QOverload<int>::of(&QComboBox::currentIndexChanged), this, [this](int idx) {
        model.setIlluminant(ColorModel::Illuminant(illuminantCombo->itemData(idx).toInt()));
    });
    connect(strategyCombo, QOverload<int>::of(&QComboBox::currentIndexChanged), this, [this](int idx) {
        model.setGamutStrategy(ColorModel::GamutStrategy(strategyCombo->itemData(idx).toInt()));
    });
    connect(hexEdit, &QLineEdit::editingFinished, this, &MainWindow::applyHex);

    connect(hueSlider, &QAbstractSlider::valueChanged, this, [this](int v) {
        if (syncing) return;
        displayHue = v / 4095.0;
        Vec3 hsl = model.hsl();
        hsl.x = displayHue;
        model.setColorFromHsl(hsl);
    });

    connect(field, &ColorField::picked, this, [this](double s, double l) {
        if (syncing) return;
        model.setColorFromHsl(Vec3(displayHue, s, l));
    });

    connect(chooseColorButton, &QPushButton::clicked, this, [this] {
        const QColor initialColor = toQColor(model.rgb());

        const QColor selectedColor = QColorDialog::getColor(
            initialColor,
            this,
            QStringLiteral("Выбрать цвет")
            );

        if (!selectedColor.isValid()) {
            return;
        }

        model.setColorFromRgb8(
            selectedColor.red(),
            selectedColor.green(),
            selectedColor.blue()
            );
    });
}

void MainWindow::onChannelEdited(int group, int channel)
{
    Q_UNUSED(channel);
    if (syncing) return;

    if (group == RGB) {
        model.setColorFromRgb(Vec3(rows[group][0]->value() / 255.0,
                                   rows[group][1]->value() / 255.0,
                                   rows[group][2]->value() / 255.0));
    } else if (group == XYZ) {
        model.setColorFromXyz(Vec3(rows[group][0]->value(),
                                   rows[group][1]->value(),
                                   rows[group][2]->value()));
    } else {
        model.setColorFromHsl(Vec3(rows[group][0]->value() / 360.0,
                                   rows[group][1]->value() / 100.0,
                                   rows[group][2]->value() / 100.0));
    }
}

void MainWindow::applyHex()
{
    QString t = hexEdit->text().remove(QLatin1Char('#'));
    if (t.size() != 6) return;
    bool ok = false;
    const int v = t.toInt(&ok, 16);
    if (!ok) return;
    model.setColorFromRgb8((v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
}

QColor MainWindow::toQColor(const Vec3& enc) const
{
    return QColor::fromRgbF(clamp01(enc.x), clamp01(enc.y), clamp01(enc.z));
}

void MainWindow::syncFromModel()
{
    syncing = true;

    const double mx = model.xyzChannelMax();
    for (int i = 0; i < 3; ++i)
        rows[XYZ][i]->setRange(0.0, mx);

    const Vec3 rgb = model.rgb();
    rows[RGB][0]->setValue(rgb.x * 255.0);
    rows[RGB][1]->setValue(rgb.y * 255.0);
    rows[RGB][2]->setValue(rgb.z * 255.0);

    const Vec3 xyz = model.xyz();
    rows[XYZ][0]->setValue(xyz.x);
    rows[XYZ][1]->setValue(xyz.y);
    rows[XYZ][2]->setValue(xyz.z);

    const Vec3 hsl = model.hsl();
    rows[HSL][0]->setValue(hsl.x * 360.0);
    rows[HSL][1]->setValue(hsl.y * 100.0);
    rows[HSL][2]->setValue(hsl.z * 100.0);
    displayHue = hsl.x;
    field->setHue(displayHue);
    field->setMarker(hsl.y, hsl.z);
    hueSlider->setValue(int(displayHue * 4095.0 + 0.5));

    preview->setStyleSheet(QStringLiteral("background:%1; border:1px solid #999;")
                               .arg(toQColor(rgb).name()));
    const Vec3 r8 = model.rgb8();
    hexEdit->setText(QStringLiteral("#%1%2%3")
                         .arg(int(r8.x), 2, 16, QLatin1Char('0'))
                         .arg(int(r8.y), 2, 16, QLatin1Char('0'))
                         .arg(int(r8.z), 2, 16, QLatin1Char('0')));

    warnLabel->setText(model.gamutWarning());

    syncing = false;
    refreshGradients();
}

QGradientStops MainWindow::makeStops(const std::function<QColor(double)>& colorAt) const
{
    const int STOPS = 64;
    QGradientStops stops;
    stops.reserve(STOPS + 1);
    for (int i = 0; i <= STOPS; ++i) {
        const double t = i / double(STOPS);
        stops.append(qMakePair(qreal(t), colorAt(t)));
    }
    return stops;
}

void MainWindow::refreshGradients()
{
    const Vec3 baseRgb = model.rgb();
    for (int ch = 0; ch < 3; ++ch)
        rows[RGB][ch]->setStops(makeStops([baseRgb, ch, this](double t) {
            Vec3 c = baseRgb; c[ch] = t; return toQColor(c);
        }));

    const Vec3 baseXyz = model.xyz();
    const double mx = model.xyzChannelMax();
    for (int ch = 0; ch < 3; ++ch)
        rows[XYZ][ch]->setStops(makeStops([baseXyz, mx, ch, this](double t) {
            Vec3 c = baseXyz; c[ch] = t * mx;
            return toQColor(model.xyzToRgb(c).encoded);
        }));

    const Vec3 baseHsl = model.hsl();
    for (int ch = 0; ch < 3; ++ch)
        rows[HSL][ch]->setStops(makeStops([baseHsl, ch](double t) {
            Vec3 c = baseHsl; c[ch] = t;
            const Vec3 e = ColorModel::hslToRgb(c);
            return QColor::fromRgbF(clamp01(e.x), clamp01(e.y), clamp01(e.z));
        }));
}