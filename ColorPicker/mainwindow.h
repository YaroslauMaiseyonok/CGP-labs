#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QVector>
#include <functional>

#include "colormodel.h"

class ChannelRow;
class ColorField;
class GradientSlider;
class QLabel;
class QLineEdit;
class QComboBox;
class QVBoxLayout;
class QPushButton;

class MainWindow : public QMainWindow
{
    Q_OBJECT
public:
    explicit MainWindow(QWidget* parent = nullptr);

private:
    enum Group { RGB = 0, XYZ, HSL, GroupCount };

    void buildUi();
    void connectAll();
    void syncFromModel();
    void refreshGradients();
    void onChannelEdited(int group, int channel);
    void applyHex();
    ChannelRow* addRow(QVBoxLayout* box, int group, const QString& name,
                       double min, double max, int decimals, const QString& suffix);

    QColor toQColor(const Vec3& encoded) const;
    QGradientStops makeStops(const std::function<QColor(double)>& colorAt) const;

    ColorModel model;

    QLabel*    preview = nullptr;
    QLineEdit* hexEdit = nullptr;
    QLabel*    warnLabel = nullptr;
    QComboBox* illuminantCombo = nullptr;
    QComboBox* strategyCombo = nullptr;
    QPushButton* chooseColorButton = nullptr;
    ColorField* field = nullptr;
    GradientSlider* hueSlider = nullptr;
    double displayHue = 0.0;

    QVector<QVector<ChannelRow*>> rows;
    bool syncing = false;
};

#endif