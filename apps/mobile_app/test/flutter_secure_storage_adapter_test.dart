import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/flutter_secure_storage_adapter.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues(<String, String>{});
  });

  test('production adapter writes, reads, and deletes secure values', () async {
    final adapter = FlutterSecureStorageAdapter();

    await adapter.write(key: 'session', value: 'secret');

    expect(await adapter.read(key: 'session'), 'secret');

    await adapter.delete(key: 'session');

    expect(await adapter.read(key: 'session'), isNull);
  });
}
