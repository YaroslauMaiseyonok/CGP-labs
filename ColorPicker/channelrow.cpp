#include "channelrow.h"
#include "gradientslider.h"

#include <QLabel>
#include <QDoubleSpinBox>
#include <QHBoxLayout>

ChannelRow::ChannelRow(const QString& name, double min, double max,
                       int decimals, const QString& suffix, QWidget* parent)
    : QWidget(parent), min(min), max(max)
{
    auto* lay = new QHBoxLayout(this);
    lay->setContentsMargins(0, 0, 0, 0);
    lay->setSpacing(6);

    label = new QLabel(name, this);
    label->setFixedWidth(16);

    slider = new GradientSlider(this);
    slider->setRange(0, SLIDER_STEPS);

    spin = new QDoubleSpinBox(this);
    spin->setDecimals(decimals);
    spin->setSuffix(suffix);
    spin->setRange(min, max);
    spin->setSingleStep((max - min) / 100.0);
    spin->setFixedWidth(90);

    lay->addWidget(label);
    lay->addWidget(slider, 1);
    lay->addWidget(spin);

    connect(slider, &QAbstractSlider::valueChanged, this, [this](int i) {
        if (sync) return;
        const double v = fromSlider(i);
        sync = true;
        spin->setValue(v);
        sync = false;
        emit valueChanged(v);
    });
    connect(spin, &QDoubleSpinBox::valueChanged, this, [this](double v) {
        if (sync) return;
        sync = true;
        slider->setValue(toSlider(v));
        sync = false;
        emit valueChanged(v);
    });
}

double ChannelRow::fromSlider(int i) const
{
    return min + (i / double(SLIDER_STEPS)) * (max - min);
}

int ChannelRow::toSlider(double v) const
{
    const double t = (max > min) ? (v - min) / (max - min) : 0.0;
    return int(qBound(0.0, t, 1.0) * SLIDER_STEPS + 0.5);
}

double ChannelRow::value() const { return spin->value(); }

void ChannelRow::setValue(double v)
{
    sync = true;
    spin->setValue(qBound(min, v, max));
    slider->setValue(toSlider(v));
    sync = false;
}

void ChannelRow::setRange(double min, double max)
{
    this->min = min;
    this->max = max;
    spin->setRange(min, max);
    spin->setSingleStep((max - min) / 100.0);
    sync = true;
    slider->setValue(toSlider(spin->value()));
    sync = false;
}

void ChannelRow::setStops(const QGradientStops& s) { slider->setStops(s); }