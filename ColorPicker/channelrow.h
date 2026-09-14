#ifndef CHANNELROW_H
#define CHANNELROW_H

#include <QWidget>
#include <QGradientStops>

class QLabel;
class QDoubleSpinBox;
class GradientSlider;

class ChannelRow : public QWidget
{
    Q_OBJECT
public:
    ChannelRow(const QString& name, double min, double max,
               int decimals, const QString& suffix, QWidget* parent = nullptr);

    double value() const;
    void   setValue(double v);
    void   setRange(double min, double max);
    void   setStops(const QGradientStops& s);

signals:
    void valueChanged(double v);

private:
    double fromSlider(int i) const;
    int    toSlider(double v) const;

    QLabel*          label = nullptr;
    GradientSlider*  slider = nullptr;
    QDoubleSpinBox*  spin = nullptr;
    double min = 0.0, max = 1.0;
    bool   sync = false;

    static constexpr int SLIDER_STEPS = 4096;
};

#endif