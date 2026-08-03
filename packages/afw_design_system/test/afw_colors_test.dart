import 'package:afw_design_system/afw_design_system.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('primary emerald token remains stable', () {
    expect(AfwColors.emerald.toARGB32(), 0xFF0F8B6D);
  });
}
